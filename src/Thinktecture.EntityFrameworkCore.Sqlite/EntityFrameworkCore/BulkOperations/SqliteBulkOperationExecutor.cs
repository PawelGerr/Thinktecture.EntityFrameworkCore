using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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
   // ReSharper disable once ClassNeverInstantiated.Global
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqliteBulkOperationExecutor
      : IBulkInsertExecutor, ITempTableBulkInsertExecutor, IBulkUpdateExecutor, ITruncateTableExecutor
   {
      private readonly DbContext _ctx;
      private readonly IDiagnosticsLogger<SqliteDbLoggerCategory.BulkOperation> _logger;
      private readonly ISqlGenerationHelper _sqlGenerationHelper;

      private static class EventIds
      {
         public static readonly EventId Started = 0;
         public static readonly EventId Finished = 1;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteBulkOperationExecutor"/>.
      /// </summary>
      /// <param name="ctx">Current database context.</param>
      /// <param name="logger">Logger.</param>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      public SqliteBulkOperationExecutor(
         ICurrentDbContext ctx,
         IDiagnosticsLogger<SqliteDbLoggerCategory.BulkOperation> logger,
         ISqlGenerationHelper sqlGenerationHelper)
      {
         _ctx = ctx?.Context ?? throw new ArgumentNullException(nameof(ctx));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      }

      /// <inheritdoc />
      IBulkInsertOptions IBulkInsertExecutor.CreateOptions()
      {
         return new SqliteBulkInsertOptions();
      }

      /// <inheritdoc />
      ITempTableBulkInsertOptions ITempTableBulkInsertExecutor.CreateOptions()
      {
         return new SqliteTempTableBulkInsertOptions();
      }

      /// <inheritdoc />
      IBulkUpdateOptions IBulkUpdateExecutor.CreateOptions()
      {
         return new SqliteBulkUpdateOptions();
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

         if (!(options is ISqliteBulkInsertOptions sqliteOptions))
            sqliteOptions = new SqliteBulkInsertOptions(options);

         var properties = sqliteOptions.MembersToInsert.GetPropertiesForInsert(entityType);
         var autoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;

         await ExecuteBulkOperationAsync(entities, schema, tableName, SqliteCommandBuilder.Insert, properties, autoIncrementBehavior, cancellationToken);
      }

      private async Task<int> ExecuteBulkOperationAsync<T>(
         IEnumerable<T> entities,
         string? schema,
         string tableName,
         SqliteCommandBuilder commandBuilder,
         IReadOnlyList<IProperty> properties,
         SqliteAutoIncrementBehavior autoIncrementBehavior,
         CancellationToken cancellationToken)
         where T : class
      {
         var factory = _ctx.GetService<IEntityDataReaderFactory>();
         var sqlCon = (SqliteConnection)_ctx.Database.GetDbConnection();

         using var reader = factory.Create(_ctx, entities, properties);

         await _ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            await using var command = sqlCon.CreateCommand();

            var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(tableName, schema);
#pragma warning disable CA2100
            command.CommandText = commandBuilder.GetStatement(_sqlGenerationHelper, reader, tableIdentifier);
#pragma warning restore CA2100
            var parameterInfos = CreateParameters(reader, command);

            try
            {
               command.Prepare();
            }
            catch (SqliteException ex)
            {
               throw new InvalidOperationException($"Cannot access destination table '{tableIdentifier}'.", ex);
            }

            LogBulkOperationStart(command.CommandText);
            var stopwatch = Stopwatch.StartNew();
            var numberOfAffectedRows = 0;

            while (reader.Read())
            {
               for (var i = 0; i < reader.FieldCount; i++)
               {
                  var paramInfo = parameterInfos[i];
                  var value = reader.GetValue(i);

                  if (autoIncrementBehavior == SqliteAutoIncrementBehavior.SetZeroToNull && paramInfo.IsAutoIncrementColumn && 0.Equals(value))
                     value = null;

                  paramInfo.Parameter.Value = value ?? DBNull.Value;
               }

               numberOfAffectedRows += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            LogBulkOperationEnd(command.CommandText, stopwatch.Elapsed);

            return numberOfAffectedRows;
         }
         finally
         {
            await _ctx.Database.CloseConnectionAsync().ConfigureAwait(false);
         }
      }

      private static ParameterInfo[] CreateParameters(IEntityDataReader reader, SqliteCommand command)
      {
         var parameters = new ParameterInfo[reader.Properties.Count];

         for (var i = 0; i < reader.Properties.Count; i++)
         {
            var property = reader.Properties[i];
            var index = reader.GetPropertyIndex(property);

            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$p{index}";
            parameters[i] = new ParameterInfo(parameter, property.IsAutoIncrement());
            command.Parameters.Add(parameter);
         }

         return parameters;
      }

      private void LogBulkOperationStart(string statement)
      {
         _logger.Logger.LogDebug(EventIds.Started, @"Executing DbCommand
{Statement}", statement);
      }

      private void LogBulkOperationEnd(string statement, TimeSpan duration)
      {
         _logger.Logger.LogInformation(EventIds.Finished, @"Executed DbCommand ({Duration}ms)
{Statement}", (long)duration.TotalMilliseconds, statement);
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
         var tempTableCreator = _ctx.GetService<ITempTableCreator>();

         if (options is not ISqliteTempTableBulkInsertOptions)
         {
            var sqliteOptions = new SqliteTempTableBulkInsertOptions(options);
            options = sqliteOptions;
         }

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(entityType, options.TempTableCreationOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            await BulkInsertAsync(entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

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
         var truncateStatement = $"DELETE FROM {tableIdentifier};";

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

         if (!(options is ISqliteBulkUpdateOptions sqliteOptions))
            sqliteOptions = new SqliteBulkUpdateOptions(options);

         var entityType = _ctx.Model.GetEntityType(typeof(T));
         var propertiesToUpdate = sqliteOptions.MembersToUpdate.GetPropertiesForUpdate(entityType);
         var keyProperties = options.KeyProperties.GetKeyProperties(entityType, propertiesToUpdate);

         return await ExecuteBulkOperationAsync(entities, entityType.GetSchema(), entityType.GetTableName(), SqliteCommandBuilder.Update(keyProperties), propertiesToUpdate, SqliteAutoIncrementBehavior.KeepValueAsIs, cancellationToken);
      }

      private readonly struct ParameterInfo
      {
         public readonly SqliteParameter Parameter;
         public readonly bool IsAutoIncrementColumn;

         public ParameterInfo(SqliteParameter parameter, bool isAutoIncrementColumn)
         {
            Parameter = parameter;
            IsAutoIncrementColumn = isAutoIncrementColumn;
         }
      }
   }
}
