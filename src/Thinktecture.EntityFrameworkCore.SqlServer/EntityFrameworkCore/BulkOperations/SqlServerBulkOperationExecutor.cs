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
   public sealed class SqlServerBulkOperationExecutor
      : IBulkInsertExecutor, ITempTableBulkInsertExecutor, IBulkUpdateExecutor, ITruncateTableExecutor
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
      IBulkInsertOptions IBulkInsertExecutor.CreateOptions()
      {
         return new SqlServerBulkInsertOptions();
      }

      /// <inheritdoc />
      ITempTableBulkInsertOptions ITempTableBulkInsertExecutor.CreateOptions()
      {
         return new SqlServerTempTableBulkInsertOptions();
      }

      /// <inheritdoc />
      IBulkUpdateOptions IBulkUpdateExecutor.CreateOptions()
      {
         return new SqlServerBulkUpdateOptions();
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

         if (options is not ISqlServerBulkInsertOptions sqlServerOptions)
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
            var columnName = property.GetColumnBaseName();

            bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(index, columnName));

            if (columnsSb.Length > 0)
               columnsSb.Append(", ");

            columnsSb.Append(columnName).Append(' ').Append(property.GetColumnType());
         }

         return columnsSb.ToString();
      }

      private SqlBulkCopy CreateSqlBulkCopy(SqlConnection sqlCon, SqlTransaction? sqlTx, string? schema, string tableName, ISqlServerBulkInsertOptions sqlServerOptions)
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
         _logger.Logger.LogDebug(EventIds.Inserting, @"Executing DbCommand [SqlBulkCopyOptions={SqlBulkCopyOptions}, BulkCopyTimeout={BulkCopyTimeout}, BatchSize={BatchSize}, EnableStreaming={EnableStreaming}]
INSERT BULK {Table} ({Columns})", options, bulkCopy.BulkCopyTimeout, bulkCopy.BatchSize, bulkCopy.EnableStreaming,
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
      public Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
         IEnumerable<T> entities,
         ITempTableBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         if (options is not ISqlServerTempTableBulkInsertOptions sqlServerOptions)
            sqlServerOptions = new SqlServerTempTableBulkInsertOptions(options);

         return BulkInsertIntoTempTableAsync(entities, sqlServerOptions, cancellationToken);
      }

      /// <summary>
      /// Inserts the provided <paramref name="entities"/> into a temp table.
      /// </summary>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entities.</typeparam>
      /// <returns>A query returning the inserted <paramref name="entities"/>.</returns>
      public async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
         IEnumerable<T> entities,
         ISqlServerTempTableBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var entityType = _ctx.Model.GetEntityType(typeof(T));
         var tempTableCreator = _ctx.GetService<ISqlServerTempTableCreator>();

         var tempTableOptions = options.TempTableCreationOptions;

         if (options.MomentOfPrimaryKeyCreation == MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert)
            tempTableOptions = new SqlServerTempTableCreationOptions(tempTableOptions) { PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(entityType, tempTableOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            await BulkInsertAsync(entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

            if (options.MomentOfPrimaryKeyCreation == MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert)
            {
               var properties = options.TempTableCreationOptions.MembersToInclude.GetPropertiesForTempTable(entityType);
               var keyProperties = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, properties);
               await tempTableCreator.CreatePrimaryKeyAsync(_ctx, keyProperties, tempTableReference.Name, options.TempTableCreationOptions.TruncateTableIfExists, cancellationToken).ConfigureAwait(false);
            }

            var query = _ctx.Set<T>().FromSqlRaw($"SELECT * FROM {_sqlGenerationHelper.DelimitIdentifier(tempTableReference.Name)}");

            return new TempTableQuery<T>(query, tempTableReference);
         }
         catch (Exception)
         {
            await tempTableReference.DisposeAsync().ConfigureAwait(false);
            throw;
         }
      }

      /// <inheritdoc />
      public async Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
         where T : class
      {
         var entityType = _ctx.Model.GetEntityType(typeof(T));
         var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(entityType.GetTableName(), entityType.GetSchema());
         var truncateStatement = $"TRUNCATE TABLE {tableIdentifier};";

         await _ctx.Database.ExecuteSqlRawAsync(truncateStatement, cancellationToken);
      }

      /// <inheritdoc />
      public async Task<int> BulkUpdateAsync<T>(
         IEnumerable<T> entities,
         IBulkUpdateOptions options,
         CancellationToken cancellationToken = default)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         if (options is not ISqlServerBulkUpdateOptions sqlServerOptions)
            sqlServerOptions = new SqlServerBulkUpdateOptions(options);

         var entityType = _ctx.Model.GetEntityType(typeof(T));
         var propertiesForUpdate = options.MembersToUpdate.GetPropertiesForUpdate(entityType);

         if (propertiesForUpdate.Count == 0)
            throw new ArgumentException("The number of properties to update cannot be 0.");

         await using var tempTable = await BulkInsertIntoTempTableAsync(entities, sqlServerOptions.TempTableOptions, cancellationToken);

         var mergeStatement = CreateMergeCommand(entityType, tempTable.Name, sqlServerOptions, propertiesForUpdate);

         return await _ctx.Database.ExecuteSqlRawAsync(mergeStatement, cancellationToken);
      }

      private string CreateMergeCommand(
         IEntityType entityType,
         string sourceTempTableName,
         ISqlServerBulkUpdateOptions options,
         IReadOnlyList<IProperty> propertiesToUpdate)
      {
         var keyProperties = GetKeyPropertiesForMerge(entityType, propertiesToUpdate, options.KeyProperties);

         var sb = new StringBuilder();

         sb.Append("MERGE INTO ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(entityType.GetTableName(), entityType.GetSchema()));

         if (options.MergeTableHints.Count != 0)
         {
            sb.Append(" WITH (");

            for (var i = 0; i < options.MergeTableHints.Count; i++)
            {
               if (i != 0)
                  sb.Append(", ");

               sb.Append(options.MergeTableHints[i]);
            }

            sb.Append(")");
         }

         sb.Append(" AS d USING ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(sourceTempTableName))
           .Append(" AS s ON ");

         var isFirstIteration = true;

         foreach (var property in keyProperties)
         {
            if (!isFirstIteration)
               sb.AppendLine(" AND ");

            var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(property.GetColumnBaseName());

            sb.Append("(d.").Append(escapedColumnName).Append(" = s.").Append(escapedColumnName);

            if (property.IsNullable)
               sb.Append(" OR d.").Append(escapedColumnName).Append(" IS NULL AND s.").Append(escapedColumnName).Append(" IS NULL");

            sb.Append(")");
            isFirstIteration = false;
         }

         sb.AppendLine()
           .AppendLine("WHEN MATCHED THEN UPDATE SET");

         isFirstIteration = true;

         foreach (var property in propertiesToUpdate.Except(keyProperties))
         {
            if (!isFirstIteration)
               sb.AppendLine(",");

            var columnName = property.GetColumnBaseName();
            var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);

            sb.Append("\td.").Append(escapedColumnName).Append(" = s.").Append(escapedColumnName);
            isFirstIteration = false;
         }

         sb.Append(_sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }

      private static IReadOnlyCollection<IProperty> GetKeyPropertiesForMerge(
         IEntityType entityType,
         IReadOnlyList<IProperty> properties,
         IEntityMembersProvider? keyMembersProvider)
      {
         var keyProperties = keyMembersProvider is null
                                ? entityType.FindPrimaryKey()?.Properties
                                : keyMembersProvider.GetProperties(entityType);

         if (keyProperties is null or { Count: 0 })
            throw new ArgumentException("The number of key properties to perform JOIN/match on cannot be 0.");

         var missingColumns = keyProperties.Except(properties);

         if (missingColumns.Any())
         {
            throw new InvalidOperationException(@$"Cannot execute MERGE command because not all key columns are part of the source table.
Missing columns: {String.Join(", ", missingColumns.Select(c => c.GetColumnBaseName()))}.");
         }

         return keyProperties;
      }
   }
}
