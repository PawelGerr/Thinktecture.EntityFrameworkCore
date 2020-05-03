using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqlServerBulkOperationExecutor : IBulkOperationExecutor, ITempTableBulkOperationExecutor
   {
      private readonly DbContext _ctx;
      private readonly IDiagnosticsLogger<SqlServerDbLoggerCategory.BulkOperation> _logger;
      private readonly ISqlGenerationHelper _sqlGenerationHelper;

      private static class EventIds
      {
         public static readonly EventId Inserting = 0;
         public static readonly EventId Inserted = 1;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerBulkOperationExecutor"/>.
      /// </summary>
      /// <param name="ctx">Current database context.</param>
      /// <param name="logger">Logger.</param>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      public SqlServerBulkOperationExecutor(
         ICurrentDbContext ctx,
         IDiagnosticsLogger<SqlServerDbLoggerCategory.BulkOperation> logger,
         ISqlGenerationHelper sqlGenerationHelper)
      {
         _ctx = ctx?.Context ?? throw new ArgumentNullException(nameof(ctx));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      }

      /// <inheritdoc />
      IBulkInsertOptions IBulkOperationExecutor.CreateOptions()
      {
         return new SqlServerBulkInsertOptions();
      }

      /// <inheritdoc />
      ITempTableBulkInsertOptions ITempTableBulkOperationExecutor.CreateOptions()
      {
         return new SqlServerTempTableBulkInsertOptions();
      }

      /// <inheritdoc />
      public Task BulkInsertAsync<T>(
         IEnumerable<T> entities,
         IBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         var entityType = _ctx.Model.GetEntityType(typeof(T));

         return BulkInsertAsync(entityType, entities, entityType.GetSchema(), entityType.GetTableName(), options, cancellationToken);
      }

      private async Task BulkInsertAsync<T>(
         IEntityType entityType,
         IEnumerable<T> entities,
         string? schema,
         string tableName,
         IBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         if (!(options is SqlServerBulkInsertOptions sqlServerOptions))
            sqlServerOptions = new SqlServerBulkInsertOptions(options);

         var factory = _ctx.GetService<IEntityDataReaderFactory>();
         var properties = options.MembersToInsert.GetPropertiesForInsert(entityType);
         var sqlCon = (SqlConnection)_ctx.Database.GetDbConnection();
         var sqlTx = (SqlTransaction?)_ctx.Database.CurrentTransaction?.GetDbTransaction();

         using var reader = factory.Create(_ctx, entities, properties);
         using var bulkCopy = CreateSqlBulkCopy(sqlCon, sqlTx, schema, tableName, sqlServerOptions);

         var columns = SetColumnMappings(bulkCopy, reader);

         await _ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            LogInserting(sqlServerOptions.SqlBulkCopyOptions, bulkCopy, columns);
            var stopwatch = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

            LogInserted(sqlServerOptions.SqlBulkCopyOptions, stopwatch.Elapsed, bulkCopy, columns);
         }
         finally
         {
            await _ctx.Database.CloseConnectionAsync().ConfigureAwait(false);
         }
      }

      private static string SetColumnMappings(SqlBulkCopy bulkCopy, IEntityDataReader reader)
      {
         var columnsSb = new StringBuilder();

         for (var i = 0; i < reader.Properties.Count; i++)
         {
            var property = reader.Properties[i];
            var index = reader.GetPropertyIndex(property);
            var columnName = property.GetColumnName();

            bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(index, columnName));

            if (columnsSb.Length > 0)
               columnsSb.Append(", ");

            columnsSb.Append(columnName).Append(" ").Append(property.GetColumnType());
         }

         return columnsSb.ToString();
      }

      private SqlBulkCopy CreateSqlBulkCopy(SqlConnection sqlCon, SqlTransaction? sqlTx, string? schema, string tableName, SqlServerBulkInsertOptions sqlServerOptions)
      {
         var bulkCopy = new SqlBulkCopy(sqlCon, sqlServerOptions.SqlBulkCopyOptions, sqlTx)
                        {
                           DestinationTableName = _sqlGenerationHelper.DelimitIdentifier(tableName, schema),
                           EnableStreaming = sqlServerOptions.EnableStreaming
                        };

         if (sqlServerOptions.BulkCopyTimeout.HasValue)
            bulkCopy.BulkCopyTimeout = (int)sqlServerOptions.BulkCopyTimeout.Value.TotalSeconds;

         if (sqlServerOptions.BatchSize.HasValue)
            bulkCopy.BatchSize = sqlServerOptions.BatchSize.Value;

         return bulkCopy;
      }

      private void LogInserting(SqlBulkCopyOptions options, SqlBulkCopy bulkCopy, string columns)
      {
         _logger.Logger.LogInformation(EventIds.Inserting, @"Executing DbCommand [SqlBulkCopyOptions={SqlBulkCopyOptions}, BulkCopyTimeout={BulkCopyTimeout}, BatchSize={BatchSize}, EnableStreaming={EnableStreaming}]
INSERT BULK {table} ({columns})", options, bulkCopy.BulkCopyTimeout, bulkCopy.BatchSize, bulkCopy.EnableStreaming,
                                       bulkCopy.DestinationTableName, columns);
      }

      private void LogInserted(SqlBulkCopyOptions options, TimeSpan duration, SqlBulkCopy bulkCopy, string columns)
      {
         _logger.Logger.LogInformation(EventIds.Inserted, @"Executed DbCommand ({duration}ms) [SqlBulkCopyOptions={SqlBulkCopyOptions}, BulkCopyTimeout={BulkCopyTimeout}, BatchSize={BatchSize}, EnableStreaming={EnableStreaming}]
INSERT BULK {table} ({columns})", (long)duration.TotalMilliseconds,
                                       options, bulkCopy.BulkCopyTimeout, bulkCopy.BatchSize, bulkCopy.EnableStreaming,
                                       bulkCopy.DestinationTableName, columns);
      }

      /// <inheritdoc />
      public async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
         IEnumerable<T> entities,
         ITempTableBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var entityType = _ctx.Model.GetEntityType(typeof(T));
         var tempTableCreator = _ctx.GetService<ISqlServerTempTableCreator>();

         if (!(options is SqlServerTempTableBulkInsertOptions sqlServerOptions))
         {
            sqlServerOptions = new SqlServerTempTableBulkInsertOptions(options);
            options = sqlServerOptions;
         }

         var tempTableOptions = options.TempTableCreationOptions;

         if (sqlServerOptions.PrimaryKeyCreation == SqlServerPrimaryKeyCreation.AfterBulkInsert && tempTableOptions.CreatePrimaryKey)
            tempTableOptions = new TempTableCreationOptions(tempTableOptions) { CreatePrimaryKey = false };

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(entityType, tempTableOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            await BulkInsertAsync(entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

            if (sqlServerOptions.PrimaryKeyCreation == SqlServerPrimaryKeyCreation.AfterBulkInsert)
               await tempTableCreator.CreatePrimaryKeyAsync(_ctx, entityType, tempTableReference.Name, options.TempTableCreationOptions.TruncateTableIfExists, cancellationToken).ConfigureAwait(false);

            var query = _ctx.Set<T>().FromSqlRaw($"SELECT * FROM {_sqlGenerationHelper.DelimitIdentifier(tempTableReference.Name)}");

            return new TempTableQuery<T>(query, tempTableReference);
         }
         catch (Exception)
         {
            await tempTableReference.DisposeAsync().ConfigureAwait(false);
            throw;
         }
      }
   }
}
