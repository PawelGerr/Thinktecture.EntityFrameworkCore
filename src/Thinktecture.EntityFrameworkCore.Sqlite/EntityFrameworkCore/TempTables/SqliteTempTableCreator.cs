using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.ObjectPool;
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
   private readonly ObjectPool<StringBuilder> _stringBuilderPool;
   private readonly TempTableStatementCache<SqliteTempTableCreatorCacheKey> _cache;

   /// <summary>
   /// Initializes <see cref="SqliteTempTableCreator"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mappings.</param>
   /// <param name="cache">SQL statement cache.</param>
   /// <param name="stringBuilderPool">String builder pool.</param>
   public SqliteTempTableCreator(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      ObjectPool<StringBuilder> stringBuilderPool,
      TempTableStatementCache<SqliteTempTableCreatorCacheKey> cache)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context;
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
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

      var nameLease = options.TableNameProvider.LeaseName(_ctx, entityType);

      try
      {
         var sql = GetTempTableCreationSql(entityType, nameLease.Name, options);

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

         return new SqliteTempTableReference(_logger, _sqlGenerationHelper, nameLease.Name, _ctx.Database, nameLease, options.DropTableOnDispose);
      }
      catch (Exception)
      {
         nameLease.Dispose();
         throw;
      }
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
      var sb = _stringBuilderPool.Get();

      try
      {
         var isFirst = true;
         var createPk = true;
         StoreObjectIdentifier? storeObject = null;

         foreach (var property in options.Properties)
         {
            if (!isFirst)
               sb.AppendLine(",");

            storeObject ??= property.GetStoreObject();
            var columnName = property.GetColumnName(storeObject.Value);

            sb.Append("\t\t")
              .Append(_sqlGenerationHelper.DelimitIdentifier(columnName)).Append(' ')
              .Append(property.Property.GetColumnType(storeObject.Value))
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

            var defaultValueSql = property.Property.GetDefaultValueSql(storeObject.Value);

            if (!String.IsNullOrWhiteSpace(defaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(defaultValueSql).Append(')');
            }
            else if (property.Property.TryGetDefaultValue(storeObject.Value, out var defaultValue))
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
      finally
      {
         _stringBuilderPool.Return(sb);
      }
   }

   private void CreatePkClause(IReadOnlyCollection<PropertyWithNavigations> keyProperties, StringBuilder sb)
   {
      if (!keyProperties.Any())
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
}
