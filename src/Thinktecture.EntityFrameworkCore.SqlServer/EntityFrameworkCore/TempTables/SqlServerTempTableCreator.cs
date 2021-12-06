using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

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
   private readonly TempTableStatementCache<SqlServerTempTableCreatorCacheKey> _tempTableCache;
   private readonly TempTableStatementCache<SqlServerTempTablePrimaryKeyCacheKey> _primaryKeyCache;

   /// <summary>
   /// Initializes <see cref="SqlServerTempTableCreator"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mappings.</param>
   /// <param name="cache">SQL statement cache.</param>
   /// <param name="primaryKeyCache">Cache for primary keys.</param>
   public SqlServerTempTableCreator(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      TempTableStatementCache<SqlServerTempTableCreatorCacheKey> cache,
      TempTableStatementCache<SqlServerTempTablePrimaryKeyCacheKey> primaryKeyCache)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _tempTableCache = cache ?? throw new ArgumentNullException(nameof(cache));
      _primaryKeyCache = primaryKeyCache ?? throw new ArgumentNullException(nameof(primaryKeyCache));
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
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(options);

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
      ArgumentNullException.ThrowIfNull(nameProvider);

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
      ArgumentNullException.ThrowIfNull(ctx);
      ArgumentNullException.ThrowIfNull(keyProperties);
      ArgumentNullException.ThrowIfNull(tableName);

      if (keyProperties.Count == 0)
         return;

      var cachedStatement = _primaryKeyCache.GetOrAdd(new SqlServerTempTablePrimaryKeyCacheKey(keyProperties, checkForExistence), CreatePrimaryKeyStatement);
      var sql = cachedStatement.GetSqlStatement(tableName);

      await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
   }

   private CachedTempTableStatement CreatePrimaryKeyStatement(SqlServerTempTablePrimaryKeyCacheKey cacheKey)
   {
      var columnNames = cacheKey.KeyProperties.Select(p =>
                                                      {
                                                         var storeObject = StoreObjectIdentifier.Create(p.Property.DeclaringEntityType, StoreObjectType.Table)
                                                                           ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.Property.DeclaringEntityType.Name}'.");
                                                         return p.Property.GetColumnName(storeObject);
                                                      });

      var commaSeparatedColumns = String.Join(", ", columnNames);

      if (cacheKey.CheckForExistence)
      {
         return new CachedTempTableStatement(delegate(string name)
                                             {
                                                var escapedTableName = _sqlGenerationHelper.DelimitIdentifier(name);
                                                return $@"
IF(NOT EXISTS (SELECT * FROM tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'PRIMARY KEY' AND OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{escapedTableName}')))
BEGIN
   ALTER TABLE {_sqlGenerationHelper.DelimitIdentifier(escapedTableName)}
   ADD CONSTRAINT {_sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY CLUSTERED ({commaSeparatedColumns});
END
";
                                             });
      }

      return new CachedTempTableStatement(name => $@"
ALTER TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
ADD CONSTRAINT {_sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY CLUSTERED ({commaSeparatedColumns});
");
   }

   private string GetTempTableCreationSql(IEntityType entityType, string tableName, ISqlServerTempTableCreationOptions options)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      var cachedStatement = _tempTableCache.GetOrAdd(new SqlServerTempTableCreatorCacheKey(options, entityType), CreateCachedStatement);

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
{columnDefinitions}
);");
      }

      return new CachedTempTableStatement(name => $@"
IF(OBJECT_ID('tempdb..{name}') IS NOT NULL)
   TRUNCATE TABLE {_sqlGenerationHelper.DelimitIdentifier(name)};
ELSE
BEGIN
CREATE TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
(
{columnDefinitions}
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

         var columnName = property.Property.GetColumnName(storeObject)
                          ?? throw new InvalidOperationException($"The property '{property.Property.Name}' has no column name.");

         sb.Append('\t')
           .Append(_sqlGenerationHelper.DelimitIdentifier(columnName)).Append(' ')
           .Append(columnType);

         if (options.UseDefaultDatabaseCollation && _stringColumnTypes.Any(t => columnType.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            sb.Append(" COLLATE database_default");

         sb.Append(property.Property.IsNullable ? " NULL" : " NOT NULL");

         if (IsIdentityColumn(property))
            sb.Append(" IDENTITY");

         var defaultValueSql = property.Property.GetDefaultValueSql(storeObject);

         if (!String.IsNullOrWhiteSpace(defaultValueSql))
         {
            sb.Append(" DEFAULT (").Append(defaultValueSql).Append(')');
         }
         else if (property.Property.TryGetDefaultValue(storeObject, out var defaultValue))
         {
            var mappingForValue = _typeMappingSource.GetMappingForValue(defaultValue);
            sb.Append(" DEFAULT ").Append(mappingForValue.GenerateSqlLiteral(defaultValue));
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

                                                return p.Property.GetColumnName(storeObject) ?? throw new Exception($"The property '{p.Property.Name}' has no column name.");
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
