using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqlServerTempTableCreator : ISqlServerTempTableCreator
   {
      private static readonly string[] _stringColumnTypes = { "char", "varchar", "text", "nchar", "nvarchar", "ntext" };

      private readonly DbContext _ctx;
      private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;
      private readonly TempTableStatementCache<SqlServerTempTableCreatorCacheKey> _cache;

      /// <summary>
      /// Initializes <see cref="SqlServerTempTableCreator"/>.
      /// </summary>
      /// <param name="ctx">Current database context.</param>
      /// <param name="logger">Logger.</param>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      /// <param name="cache">SQL statement cache.</param>
      public SqlServerTempTableCreator(
         ICurrentDbContext ctx,
         IDiagnosticsLogger<DbLoggerCategory.Query> logger,
         ISqlGenerationHelper sqlGenerationHelper,
         IRelationalTypeMappingSource typeMappingSource,
         TempTableStatementCache<SqlServerTempTableCreatorCacheKey> cache)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         _ctx = ctx.Context ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
         _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      }

      /// <inheritdoc />
      public Task<ITempTableReference> CreateTempTableAsync(
         IEntityType entityType,
         ITempTableCreationOptions options,
         CancellationToken cancellationToken = default)
      {
         if (options is not ISqlServerTempTableCreationOptions sqlServerOptions)
            sqlServerOptions = new SqlServerTempTableCreationOptions(options);

         return CreateTempTableAsync(entityType, sqlServerOptions, cancellationToken);
      }

      /// <inheritdoc />
      public async Task<ITempTableReference> CreateTempTableAsync(
         IEntityType entityType,
         ISqlServerTempTableCreationOptions options,
         CancellationToken cancellationToken = default)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var (nameLease, tableName) = GetTableName(entityType, options.TableNameProvider);
         var sql = GetTempTableCreationSql(entityType, tableName, options);

         await _ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            await _ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
         }
         catch (Exception)
         {
            await _ctx.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
         }

         return new SqlServerTempTableReference(_logger, _sqlGenerationHelper, tableName, _ctx.Database, nameLease, options.DropTableOnDispose);
      }

      private (ITempTableNameLease nameLease, string tableName) GetTableName(
         IEntityType entityType,
         ITempTableNameProvider nameProvider)
      {
         if (nameProvider == null)
            throw new ArgumentNullException(nameof(nameProvider));

         var nameLease = nameProvider.LeaseName(_ctx, entityType);
         var name = nameLease.Name;

         if (!name.StartsWith("#", StringComparison.Ordinal))
            name = $"#{name}";

         return (nameLease, name);
      }

      /// <inheritdoc />
      public async Task CreatePrimaryKeyAsync(
         DbContext ctx,
         IReadOnlyCollection<PropertyWithNavigations> keyProperties,
         string tableName,
         bool checkForExistence = false,
         CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (keyProperties == null)
            throw new ArgumentNullException(nameof(keyProperties));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         if (keyProperties.Count == 0)
            return;

         var columnNames = keyProperties.Select(p =>
                                                {
                                                   var storeObject = StoreObjectIdentifier.Create(p.Property.DeclaringEntityType, StoreObjectType.Table)
                                                                     ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.Property.DeclaringEntityType.Name}'.");
                                                   return p.Property.GetColumnName(storeObject);
                                                });

         var sql = $@"
ALTER TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName)}
ADD CONSTRAINT {_sqlGenerationHelper.DelimitIdentifier($"PK_{tableName}_{Guid.NewGuid():N}")} PRIMARY KEY CLUSTERED ({String.Join(", ", columnNames)});
";

         if (checkForExistence)
         {
            sql = $@"
IF(NOT EXISTS (SELECT * FROM tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'PRIMARY KEY' AND OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{tableName}')))
BEGIN
{sql}
END
";
         }

         await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
      }

      private string GetTempTableCreationSql(IEntityType entityType, string tableName, ISqlServerTempTableCreationOptions options)
      {
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var cachedStatement = _cache.GetOrAdd(new SqlServerTempTableCreatorCacheKey(options, entityType), CreateCachedStatement);

         return cachedStatement.GetSqlStatement(tableName);
      }

      private CachedTempTableStatement CreateCachedStatement(SqlServerTempTableCreatorCacheKey cacheKey)
      {
         var columnDefinitions = GetColumnsDefinitions(cacheKey);

         if (!cacheKey.TruncateTableIfExists)
         {
            return new CachedTempTableStatement(name => $@"
CREATE TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
(
" + columnDefinitions + @"
);");
         }

         return new CachedTempTableStatement(name => $@"
IF(OBJECT_ID('tempdb..{name}') IS NOT NULL)
   TRUNCATE TABLE {_sqlGenerationHelper.DelimitIdentifier(name)};
ELSE
BEGIN
CREATE TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
(
" + columnDefinitions + @"
);
END
");
      }

      private string GetColumnsDefinitions(SqlServerTempTableCreatorCacheKey options)
      {
         var sb = new StringBuilder();
         var isFirst = true;

         foreach (var property in options.Properties)
         {
            if (!isFirst)
               sb.AppendLine(",");

            var columnType = property.Property.GetColumnType();

            var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                              ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");

            sb.Append('\t')
              .Append(_sqlGenerationHelper.DelimitIdentifier(property.Property.GetColumnName(storeObject))).Append(' ')
              .Append(columnType);

            if (options.UseDefaultDatabaseCollation && _stringColumnTypes.Any(t => columnType.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
               sb.Append(" COLLATE database_default");

            sb.Append(property.Property.IsNullable ? " NULL" : " NOT NULL");

            if (IsIdentityColumn(property))
               sb.Append(" IDENTITY");

            var defaultValueSql = property.Property.GetDefaultValueSql();

            if (!String.IsNullOrWhiteSpace(defaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(defaultValueSql).Append(')');
            }
            else
            {
               var defaultValue = property.Property.GetDefaultValue();

               if (defaultValue != null && defaultValue != DBNull.Value)
               {
                  var mappingForValue = _typeMappingSource.GetMappingForValue(defaultValue);
                  sb.Append(" DEFAULT ").Append(mappingForValue.GenerateSqlLiteral(defaultValue));
               }
            }

            isFirst = false;
         }

         CreatePkClause(options.PrimaryKeys, sb);

         return sb.ToString();
      }

      private void CreatePkClause(
         IReadOnlyCollection<PropertyWithNavigations> keyProperties,
         StringBuilder sb)
      {
         if (keyProperties.Count <= 0)
            return;

         var columnNames = keyProperties.Select(p =>
                                                {
                                                   var storeObject = StoreObjectIdentifier.Create(p.Property.DeclaringEntityType, StoreObjectType.Table)
                                                                     ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.Property.DeclaringEntityType.Name}'.");

                                                   return p.Property.GetColumnName(storeObject);
                                                });

         sb.AppendLine(",");
         sb.Append("\t\tPRIMARY KEY (");
         var isFirst = true;

         foreach (var columnName in columnNames)
         {
            if (!isFirst)
               sb.Append(", ");

            sb.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
            isFirst = false;
         }

         sb.Append(')');
      }

      private static bool IsIdentityColumn(PropertyWithNavigations property)
      {
         return SqlServerValueGenerationStrategy.IdentityColumn.Equals(property.Property.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy)?.Value);
      }
   }
}
