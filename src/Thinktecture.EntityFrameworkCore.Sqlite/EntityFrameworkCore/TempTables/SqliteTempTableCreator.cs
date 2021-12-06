using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates temp tables.
/// </summary>
public sealed class SqliteTempTableCreator : ITempTableCreator
{
   private readonly DbContext _ctx;
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly TempTableStatementCache<SqliteTempTableCreatorCacheKey> _cache;

   /// <summary>
   /// Initializes <see cref="SqliteTempTableCreator"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mappings.</param>
   /// <param name="cache">SQL statement cache.</param>
   public SqliteTempTableCreator(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      TempTableStatementCache<SqliteTempTableCreatorCacheKey> cache)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context;
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _cache = cache ?? throw new ArgumentNullException(nameof(cache));
   }

   /// <inheritdoc />
   public async Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      ITempTableCreationOptions options,
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

      return new SqliteTempTableReference(_logger, _sqlGenerationHelper, tableName, _ctx.Database, nameLease, options.DropTableOnDispose);
   }

   private string GetTempTableCreationSql(IEntityType entityType, string tableName, ITempTableCreationOptions options)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      var cachedStatement = _cache.GetOrAdd(new SqliteTempTableCreatorCacheKey(options, entityType), CreateCachedStatement);

      return cachedStatement.GetSqlStatement(tableName);
   }

   private CachedTempTableStatement CreateCachedStatement(SqliteTempTableCreatorCacheKey cacheKey)
   {
      var columnDefinitions = GetColumnsDefinitions(cacheKey);

      if (!cacheKey.TruncateTableIfExists)
      {
         return new CachedTempTableStatement(name => $@"
      CREATE TEMPORARY TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
      (
" + columnDefinitions + @"
      );");
      }

      return new CachedTempTableStatement(name => $@"
DROP TABLE IF EXISTS {_sqlGenerationHelper.DelimitIdentifier(name, "temp")};

CREATE TEMPORARY TABLE {_sqlGenerationHelper.DelimitIdentifier(name)}
(
" + columnDefinitions + @"
);");
   }

   private string GetColumnsDefinitions(SqliteTempTableCreatorCacheKey options)
   {
      var sb = new StringBuilder();
      var isFirst = true;
      var createPk = true;

      foreach (var property in options.Properties)
      {
         if (!isFirst)
            sb.AppendLine(",");

         var storeObject = StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
                           ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
         var columnName = property.Property.GetColumnName(storeObject)
                          ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");

         sb.Append("\t\t")
           .Append(_sqlGenerationHelper.DelimitIdentifier(columnName)).Append(' ')
           .Append(property.Property.GetColumnType())
           .Append(property.Property.IsNullable ? " NULL" : " NOT NULL");

         if (property.Property.IsAutoIncrement())
         {
            if (options.PrimaryKeys.Count != 1 || !property.Equals(options.PrimaryKeys.First()))
            {
               throw new NotSupportedException(@$"SQLite does not allow the property '{property.Property.Name}' of the entity '{property.Property.DeclaringEntityType.Name}' to be an AUTOINCREMENT column unless this column is the PRIMARY KEY.
Currently configured primary keys: [{String.Join(", ", options.PrimaryKeys.Select(p => p.Property.Name))}]");
            }

            sb.Append(" PRIMARY KEY AUTOINCREMENT");
            createPk = false;
         }

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

      if (createPk)
         CreatePkClause(options.PrimaryKeys, sb);

      return sb.ToString();
   }

   private void CreatePkClause(IReadOnlyCollection<PropertyWithNavigations> keyProperties, StringBuilder sb)
   {
      if (!keyProperties.Any())
         return;

      var columnNames = keyProperties.Select(p =>
                                             {
                                                var storeObject = StoreObjectIdentifier.Create(p.Property.DeclaringEntityType, StoreObjectType.Table)
                                                                  ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.Property.DeclaringEntityType.Name}'.");

                                                return p.Property.GetColumnName(storeObject)
                                                       ?? throw new Exception($"The property '{p.Property.Name}' has no column name.");
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

   private (ITempTableNameLease nameLease, string tableName) GetTableName(
      IEntityType entityType,
      ITempTableNameProvider nameProvider)
   {
      ArgumentNullException.ThrowIfNull(nameProvider);

      var nameLease = nameProvider.LeaseName(_ctx, entityType);
      var name = nameLease.Name;

      return (nameLease, name);
   }
}
