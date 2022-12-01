using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.ObjectPool;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates temp tables.
/// </summary>
public sealed class SqlServerTempTableCreator : ISqlServerTempTableCreator
{
   private static readonly string[] _stringColumnTypes = { "char", "varchar", "text", "nchar", "nvarchar", "ntext" };

   private readonly DbContext _ctx;
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly TempTableStatementCache<SqlServerTempTableCreatorCacheKey> _tempTableCache;
   private readonly TempTableStatementCache<SqlServerTempTablePrimaryKeyCacheKey> _primaryKeyCache;
   private readonly ObjectPool<StringBuilder> _stringBuilderPool;

   /// <summary>
   /// Initializes <see cref="SqlServerTempTableCreator"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mappings.</param>
   /// <param name="cache">SQL statement cache.</param>
   /// <param name="primaryKeyCache">Cache for primary keys.</param>
   /// <param name="stringBuilderPool">String builder pool.</param>
   public SqlServerTempTableCreator(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      TempTableStatementCache<SqlServerTempTableCreatorCacheKey> cache,
      TempTableStatementCache<SqlServerTempTablePrimaryKeyCacheKey> primaryKeyCache,
      ObjectPool<StringBuilder> stringBuilderPool)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _tempTableCache = cache ?? throw new ArgumentNullException(nameof(cache));
      _primaryKeyCache = primaryKeyCache ?? throw new ArgumentNullException(nameof(primaryKeyCache));
      _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
   }

   /// <inheritdoc />
   public Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken = default)
   {
      if (options is not SqlServerTempTableCreationOptions sqlServerOptions)
         sqlServerOptions = new SqlServerTempTableCreationOptions(options);

      return CreateTempTableAsync(entityType, sqlServerOptions, cancellationToken);
   }

   /// <inheritdoc />
   public async Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      SqlServerTempTableCreationOptions options,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(options);

      var (nameLease, tableName) = GetTableName(entityType, options.TableNameProvider);

      try
      {
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
      catch (Exception)
      {
         nameLease.Dispose();
         throw;
      }
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
      IReadOnlyCollection<IProperty> keyProperties,
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
      var sql = cachedStatement.GetSqlStatement(_sqlGenerationHelper, tableName);

      await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
   }

   private ICachedTempTableStatement CreatePrimaryKeyStatement(SqlServerTempTablePrimaryKeyCacheKey cacheKey)
   {
      var columnNames = cacheKey.KeyProperties.Select(p =>
                                                      {
                                                         var storeObject = p.GetStoreObject();
                                                         return p.GetColumnName(storeObject);
                                                      });

      var commaSeparatedColumns = String.Join(", ", columnNames);

      if (cacheKey.CheckForExistence)
      {
         return new CachedTempTableStatement<string>(commaSeparatedColumns, static (sqlGenerationHelper, name, commaSeparatedColumns) => $@"
IF(NOT EXISTS (SELECT * FROM tempdb.INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'PRIMARY KEY' AND OBJECT_ID(TABLE_CATALOG + '..' + TABLE_NAME) = OBJECT_ID('tempdb..{name}')))
BEGIN
   ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
   ADD CONSTRAINT {sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY CLUSTERED ({commaSeparatedColumns});
END
");
      }

      return new CachedTempTableStatement<string>(commaSeparatedColumns, static (sqlGenerationHelper, name, commaSeparatedColumns) => $@"
ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
ADD CONSTRAINT {sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY CLUSTERED ({commaSeparatedColumns});
");
   }

   private string GetTempTableCreationSql(IEntityType entityType, string tableName, SqlServerTempTableCreationOptions options)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      var cachedStatement = _tempTableCache.GetOrAdd(new SqlServerTempTableCreatorCacheKey(options, entityType), CreateCachedStatement);

      return cachedStatement.GetSqlStatement(_sqlGenerationHelper, tableName);
   }

   private ICachedTempTableStatement CreateCachedStatement(SqlServerTempTableCreatorCacheKey cacheKey)
   {
      var columnDefinitions = GetColumnsDefinitions(cacheKey);

      if (!cacheKey.TruncateTableIfExists)
      {
         return new CachedTempTableStatement<string>(columnDefinitions,
                                                     static (sqlGenerationHelper, name, columnDefinitions) => $@"
CREATE TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
(
{columnDefinitions}
);");
      }

      return new CachedTempTableStatement<string>(columnDefinitions,
                                                  static (sqlGenerationHelper, name, columnDefinitions) =>
                                                  {
                                                     var escapedTableName = sqlGenerationHelper.DelimitIdentifier(name);

                                                     return $@"
IF(OBJECT_ID('tempdb..{name}') IS NOT NULL)
   TRUNCATE TABLE {escapedTableName};
ELSE
BEGIN
CREATE TABLE {escapedTableName}
(
{columnDefinitions}
);
END
";
                                                  });
   }

   private string GetColumnsDefinitions(SqlServerTempTableCreatorCacheKey options)
   {
      var sb = _stringBuilderPool.Get();

      try
      {
         StoreObjectIdentifier? storeObject = null;
         IEntityType? designTimeEntityType = null;

         var isFirst = true;

         foreach (var property in options.Properties)
         {
            if (!isFirst)
               sb.AppendLine(",");

            storeObject ??= property.GetStoreObject();

            var columnType = property.GetColumnType(storeObject.Value);
            var columnName = property.GetColumnName(storeObject.Value);

            sb.Append('\t')
              .Append(_sqlGenerationHelper.DelimitIdentifier(columnName)).Append(' ')
              .Append(columnType);

            if (_stringColumnTypes.Any(t => columnType.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            {
               // Collation information is not available from the runtime model, so we need to fetch it from the design time model
               designTimeEntityType ??= _ctx.GetService<IDesignTimeModel>().Model
                                            .GetEntityType(property.DeclaringEntityType.Name);

               var collation = designTimeEntityType.GetProperty(property.Name)
                                                   .GetCollation(storeObject.Value);

               if (options.UseDefaultDatabaseCollation && String.IsNullOrWhiteSpace(collation))
                  collation = "database_default";

               if (!String.IsNullOrWhiteSpace(collation))
                  sb.Append(" COLLATE ").Append(collation);
            }

            sb.Append(property.IsNullable ? " NULL" : " NOT NULL");

            if (IsIdentityColumn(property))
               sb.Append(" IDENTITY");

            var defaultValueSql = property.GetDefaultValueSql(storeObject.Value);

            if (!String.IsNullOrWhiteSpace(defaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(defaultValueSql).Append(')');
            }
            else if (property.TryGetDefaultValue(storeObject.Value, out var defaultValue) && defaultValue is not null)
            {
               var converter = property.GetValueConverter();

               if (converter is not null)
                  defaultValue = converter.ConvertToProvider(defaultValue);

               if (defaultValue is not null)
               {
                  var mappingForValue = _typeMappingSource.FindMapping(defaultValue.GetType(), columnType)
                                        ?? _typeMappingSource.GetMappingForValue(defaultValue);

                  sb.Append(" DEFAULT ").Append(mappingForValue.GenerateSqlLiteral(defaultValue));
               }
            }

            isFirst = false;
         }

         CreatePkClause(options.PrimaryKeys, sb);

         return sb.ToString();
      }
      finally
      {
         _stringBuilderPool.Return(sb);
      }
   }

   private void CreatePkClause(
      IReadOnlyCollection<IProperty> keyProperties,
      StringBuilder sb)
   {
      if (keyProperties.Count <= 0)
         return;

      var columnNames = keyProperties.Select(p =>
                                             {
                                                var storeObject = p.GetStoreObject();
                                                return p.GetColumnName(storeObject);
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

   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private static bool IsIdentityColumn(IProperty property)
   {
      return SqlServerValueGenerationStrategy.IdentityColumn.Equals(property.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy)?.Value);
   }
}
