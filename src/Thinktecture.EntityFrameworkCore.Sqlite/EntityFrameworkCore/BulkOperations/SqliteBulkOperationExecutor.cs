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
   public sealed class SqliteBulkOperationExecutor : IBulkOperationExecutor, ITempTableBulkOperationExecutor
   {
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IDiagnosticsLogger<SqliteDbLoggerCategory.BulkOperation> _logger;

      private static class EventIds
      {
         public static readonly EventId Inserting = 0;
         public static readonly EventId Inserted = 1;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteBulkOperationExecutor"/>.
      /// </summary>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="logger"></param>
      public SqliteBulkOperationExecutor(ISqlGenerationHelper sqlGenerationHelper,
                                         IDiagnosticsLogger<SqliteDbLoggerCategory.BulkOperation> logger)
      {
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      /// <inheritdoc />
      IBulkInsertOptions IBulkOperationExecutor.CreateOptions()
      {
         return new SqliteBulkInsertOptions();
      }

      /// <inheritdoc />
      ITempTableBulkInsertOptions ITempTableBulkOperationExecutor.CreateOptions()
      {
         return new SqliteTempTableBulkInsertOptions();
      }

      /// <inheritdoc />
      public Task BulkInsertAsync<T>(DbContext ctx,
                                     IEntityType entityType,
                                     IEnumerable<T> entities,
                                     IBulkInsertOptions options,
                                     CancellationToken cancellationToken = default)
         where T : class
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         return BulkInsertAsync(ctx, entityType, entities, entityType.GetSchema(), entityType.GetTableName(), options, cancellationToken);
      }

      /// <inheritdoc />
      public async Task BulkInsertAsync<T>(DbContext ctx,
                                           IEntityType entityType,
                                           IEnumerable<T> entities,
                                           string? schema,
                                           string tableName,
                                           IBulkInsertOptions options,
                                           CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         if (!(options is SqliteBulkInsertOptions sqliteOptions))
            sqliteOptions = new SqliteBulkInsertOptions(options);

         var factory = ctx.GetService<IEntityDataReaderFactory>();
         var properties = sqliteOptions.MembersToInsert.GetPropertiesForInsert(entityType);
         var sqlCon = (SqliteConnection)ctx.Database.GetDbConnection();

         using var reader = factory.Create(ctx, entities, properties);

         await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            await using var command = sqlCon.CreateCommand();

            var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(tableName, schema);
#pragma warning disable CA2100
            command.CommandText = GetInsertStatement(reader, tableIdentifier);
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

            LogInserting(command.CommandText);
            var stopwatch = Stopwatch.StartNew();

            while (reader.Read())
            {
               for (var i = 0; i < reader.FieldCount; i++)
               {
                  var paramInfo = parameterInfos[i];
                  object? value = reader.GetValue(i);

                  if (sqliteOptions.AutoIncrementBehavior == SqliteAutoIncrementBehavior.SetZeroToNull && paramInfo.IsAutoIncrementColumn && value.Equals(0))
                     value = null;

                  paramInfo.Parameter.Value = value ?? DBNull.Value;
               }

               await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            LogInserted(command.CommandText, stopwatch.Elapsed);
         }
         finally
         {
            ctx.Database.CloseConnection();
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

      private string GetInsertStatement(IEntityDataReader reader,
                                        string tableIdentifier)
      {
         var sb = new StringBuilder();
         sb.Append("INSERT INTO ").Append(tableIdentifier).Append("(");

         for (var i = 0; i < reader.Properties.Count; i++)
         {
            var column = reader.Properties[i];

            if (i > 0)
               sb.Append(", ");

            sb.Append(_sqlGenerationHelper.DelimitIdentifier(column.GetColumnName()));
         }

         sb.Append(") VALUES (");

         for (var i = 0; i < reader.Properties.Count; i++)
         {
            var property = reader.Properties[i];
            var index = reader.GetPropertyIndex(property);

            if (i > 0)
               sb.Append(", ");

            sb.Append("$p").Append(index);
         }

         sb.Append(");");

         return sb.ToString();
      }

      private void LogInserting(string insertStatement)
      {
         _logger.Logger.LogInformation(EventIds.Inserting, @"Executing DbCommand
{insertStatement}", insertStatement);
      }

      private void LogInserted(string insertStatement, TimeSpan duration)
      {
         _logger.Logger.LogInformation(EventIds.Inserted, @"Executed DbCommand ({duration}ms)
{insertStatement}", (long)duration.TotalMilliseconds, insertStatement);
      }

      /// <inheritdoc />
      public async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(DbContext ctx,
                                                                            IEnumerable<T> entities,
                                                                            ITempTableBulkInsertOptions options,
                                                                            CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var entityType = ctx.Model.GetEntityType(typeof(T));
         var tempTableCreator = ctx.GetService<ITempTableCreator>();
         var bulkInsertExecutor = ctx.GetService<IBulkOperationExecutor>();

         if (!(options is SqliteTempTableBulkInsertOptions))
         {
            var sqliteOptions = new SqliteTempTableBulkInsertOptions(options);
            options = sqliteOptions;
         }

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(ctx, entityType, options.TempTableCreationOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            await bulkInsertExecutor.BulkInsertAsync(ctx, entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

            var query = ctx.Set<T>().FromSqlRaw($"SELECT * FROM {_sqlGenerationHelper.DelimitIdentifier(tempTableReference.Name)}");

            return new TempTableQuery<T>(query, tempTableReference);
         }
         catch (Exception)
         {
            tempTableReference.Dispose();
            throw;
         }
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
