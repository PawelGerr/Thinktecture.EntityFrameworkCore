using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class SqlServerBulkOperationExecutor : IBulkOperationExecutor
   {
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IDiagnosticsLogger<SqlServerDbLoggerCategory.BulkOperation> _logger;

      private static class EventIds
      {
         public static readonly EventId Inserting = 0;
         public static readonly EventId Inserted = 1;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerBulkOperationExecutor"/>.
      /// </summary>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="logger"></param>
      public SqlServerBulkOperationExecutor(ISqlGenerationHelper sqlGenerationHelper,
                                            IDiagnosticsLogger<SqlServerDbLoggerCategory.BulkOperation> logger)
      {
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      /// <inheritdoc />
      public IBulkInsertOptions CreateOptions()
      {
         return new SqlServerBulkInsertOptions();
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

         if (!(options is SqlServerBulkInsertOptions sqlServerOptions))
         {
            sqlServerOptions = new SqlServerBulkInsertOptions();
            sqlServerOptions.InitializeFrom(options);
         }

         var factory = ctx.GetService<IEntityDataReaderFactory>();
         var properties = GetPropertiesForInsert(options.EntityMembersProvider, entityType);
         var sqlCon = (SqlConnection)ctx.Database.GetDbConnection();
         var sqlTx = (SqlTransaction?)ctx.Database.CurrentTransaction?.GetDbTransaction();

         using var reader = factory.Create(ctx, entities, properties);
         using var bulkCopy = new SqlBulkCopy(sqlCon, sqlServerOptions.SqlBulkCopyOptions, sqlTx)
                              {
                                 DestinationTableName = _sqlGenerationHelper.DelimitIdentifier(tableName, schema),
                                 EnableStreaming = sqlServerOptions.EnableStreaming
                              };

         if (sqlServerOptions.BulkCopyTimeout.HasValue)
            bulkCopy.BulkCopyTimeout = (int)sqlServerOptions.BulkCopyTimeout.Value.TotalSeconds;

         if (sqlServerOptions.BatchSize.HasValue)
            bulkCopy.BatchSize = sqlServerOptions.BatchSize.Value;

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

         await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            var columns = columnsSb.ToString();
            LogInserting(sqlServerOptions.SqlBulkCopyOptions, bulkCopy, columns);
            var stopwatch = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

            LogInserted(sqlServerOptions.SqlBulkCopyOptions, stopwatch.Elapsed, bulkCopy, columns);
         }
         finally
         {
            ctx.Database.CloseConnection();
         }
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

      private static IReadOnlyList<IProperty> GetPropertiesForInsert(IEntityMembersProvider? entityMembersProvider,
                                                                     IEntityType entityType)
      {
         if (entityMembersProvider == null)
            return entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList();

         return ConvertToEntityProperties(entityMembersProvider.GetMembers(), entityType);
      }

      private static IReadOnlyList<IProperty> ConvertToEntityProperties(IReadOnlyList<MemberInfo> memberInfos, IEntityType entityType)
      {
         var properties = new IProperty[memberInfos.Count];

         for (var i = 0; i < memberInfos.Count; i++)
         {
            var memberInfo = memberInfos[i];
            var property = FindProperty(entityType, memberInfo);

            properties[i] = property ?? throw new ArgumentException($"The member '{memberInfo.Name}' has not found on entity '{entityType.Name}'.", nameof(memberInfos));
         }

         return properties;
      }

      private static IProperty? FindProperty(IEntityType entityType, MemberInfo memberInfo)
      {
         foreach (var property in entityType.GetProperties())
         {
            if (property.PropertyInfo == memberInfo || property.FieldInfo == memberInfo)
               return property;
         }

         return null;
      }
   }
}
