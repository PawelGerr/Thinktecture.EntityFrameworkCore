using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.ObjectPool;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates temp tables for PostgreSQL.
/// </summary>
public sealed class NpgsqlTempTableCreator : INpgsqlTempTableCreator
{
   private static readonly string[] _stringColumnTypes = ["character varying", "varchar", "text", "char", "character"];

   private readonly DbContext _ctx;
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly TempTableStatementCache<NpgsqlTempTableCreatorCacheKey> _tempTableCache;
   private readonly TempTableStatementCache<NpgsqlTempTablePrimaryKeyCacheKey> _primaryKeyCache;
   private readonly ObjectPool<StringBuilder> _stringBuilderPool;

   /// <summary>
   /// Initializes <see cref="NpgsqlTempTableCreator"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mappings.</param>
   /// <param name="cache">SQL statement cache.</param>
   /// <param name="primaryKeyCache">Cache for primary keys.</param>
   /// <param name="stringBuilderPool">String builder pool.</param>
   public NpgsqlTempTableCreator(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      TempTableStatementCache<NpgsqlTempTableCreatorCacheKey> cache,
      TempTableStatementCache<NpgsqlTempTablePrimaryKeyCacheKey> primaryKeyCache,
      ObjectPool<StringBuilder> stringBuilderPool)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context ?? throw new ArgumentNullException(nameof(ctx));
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
      if (options is not NpgsqlTempTableCreationOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlTempTableCreationOptions(options);

      return CreateTempTableAsync(entityType, npgsqlOptions, cancellationToken);
   }

   /// <inheritdoc />
   public async Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      NpgsqlTempTableCreationOptions options,
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

         return new NpgsqlTempTableReference(_logger, _sqlGenerationHelper, tableName, _ctx.Database, nameLease, options.DropTableOnDispose);
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

      var cachedStatement = _primaryKeyCache.GetOrAdd(new NpgsqlTempTablePrimaryKeyCacheKey(keyProperties, checkForExistence), CreatePrimaryKeyStatement);
      var sql = cachedStatement.GetSqlStatement(_sqlGenerationHelper, tableName);

      await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
   }

   private ICachedTempTableStatement CreatePrimaryKeyStatement(NpgsqlTempTablePrimaryKeyCacheKey cacheKey)
   {
      var columnNames = cacheKey.KeyProperties.Select(p =>
                                                      {
                                                         var storeObject = p.GetStoreObject();
                                                         return _sqlGenerationHelper.DelimitIdentifier(
                                                            p.GetColumnName(storeObject)
                                                            ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.DeclaringType.Name}'."));
                                                      });

      var commaSeparatedColumns = String.Join(", ", columnNames);

      if (cacheKey.CheckForExistence)
      {
         return new CachedTempTableStatement<string>(commaSeparatedColumns,
                                                     static (sqlGenerationHelper, name, commaSeparatedColumns) =>
                                                        $"""
                                                         DO $$
                                                         BEGIN
                                                            IF NOT EXISTS (
                                                               SELECT 1 FROM pg_catalog.pg_constraint c
                                                               JOIN pg_catalog.pg_class t ON t.oid = c.conrelid
                                                               WHERE c.contype = 'p' AND t.relname = '{name}'
                                                                 AND t.relnamespace = pg_my_temp_schema()
                                                            ) THEN
                                                               ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
                                                               ADD CONSTRAINT {sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY ({commaSeparatedColumns});
                                                            END IF;
                                                         END $$;
                                                         """);
      }

      return new CachedTempTableStatement<string>(commaSeparatedColumns, static (sqlGenerationHelper, name, commaSeparatedColumns) =>
                                                                            $"""
                                                                             ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
                                                                             ADD CONSTRAINT {sqlGenerationHelper.DelimitIdentifier($"PK_{name}_{Guid.NewGuid():N}")} PRIMARY KEY ({commaSeparatedColumns});
                                                                             """);
   }

   private string GetTempTableCreationSql(IEntityType entityType, string tableName, NpgsqlTempTableCreationOptions options)
   {
      ArgumentNullException.ThrowIfNull(tableName);

      var cachedStatement = _tempTableCache.GetOrAdd(new NpgsqlTempTableCreatorCacheKey(options, entityType), CreateCachedStatement);

      return cachedStatement.GetSqlStatement(_sqlGenerationHelper, tableName);
   }

   private ICachedTempTableStatement CreateCachedStatement(NpgsqlTempTableCreatorCacheKey cacheKey)
   {
      var columnDefinitions = GetColumnsDefinitions(cacheKey);

      if (!cacheKey.TruncateTableIfExists)
      {
         return new CachedTempTableStatement<string>(columnDefinitions,
                                                     static (sqlGenerationHelper, name, columnDefinitions) =>
                                                        $"""
                                                         CREATE TEMP TABLE {sqlGenerationHelper.DelimitIdentifier(name)}
                                                         (
                                                         {columnDefinitions}
                                                         );
                                                         """);
      }

      return new CachedTempTableStatement<string>(columnDefinitions,
                                                  static (sqlGenerationHelper, name, columnDefinitions) =>
                                                  {
                                                     var escapedTableName = sqlGenerationHelper.DelimitIdentifier(name);

                                                     return $"""
                                                             CREATE TEMP TABLE IF NOT EXISTS {escapedTableName}
                                                             (
                                                             {columnDefinitions}
                                                             );
                                                             TRUNCATE TABLE {escapedTableName};
                                                             """;
                                                  });
   }

   private string GetColumnsDefinitions(NpgsqlTempTableCreatorCacheKey options)
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
            var columnName = property.GetColumnName(storeObject.Value)
                             ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.DeclaringType.Name}'.");

            sb.Append('\t')
              .Append(_sqlGenerationHelper.DelimitIdentifier(columnName)).Append(' ')
              .Append(columnType);

            if (_stringColumnTypes.Any(t => columnType.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            {
               designTimeEntityType ??= _ctx.GetService<IDesignTimeModel>().Model
                                            .GetEntityType(property.DeclaringType.Name);

               var collation = designTimeEntityType.GetProperty(property.Name)
                                                   .GetCollation(storeObject.Value);

               if (!String.IsNullOrWhiteSpace(collation))
               {
                  sb.Append(" COLLATE ");

                  string escapedCollation;

                  if (options.SplitCollationComponents)
                  {
                     var dotIndex = collation.IndexOf('.');
                     escapedCollation = dotIndex >= 0
                                           ? _sqlGenerationHelper.DelimitIdentifier(collation[(dotIndex + 1)..], collation[..dotIndex])
                                           : _sqlGenerationHelper.DelimitIdentifier(collation);
                  }
                  else
                  {
                     escapedCollation = _sqlGenerationHelper.DelimitIdentifier(collation);
                  }

                  sb.Append(escapedCollation);
               }
            }

            sb.Append(property.IsNullable ? " NULL" : " NOT NULL");

            AppendIdentity(sb, property);

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
                                                return p.GetColumnName(storeObject)
                                                       ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{p.DeclaringType.Name}'.");
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
   private static void AppendIdentity(StringBuilder sb, IProperty property)
   {
      var strategy = property.FindAnnotation(NpgsqlAnnotationNames.ValueGenerationStrategy)?.Value;

      if (strategy is NpgsqlValueGenerationStrategy npgsqlStrategy)
      {
         if (npgsqlStrategy == NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
            sb.Append(" GENERATED BY DEFAULT AS IDENTITY");
         else if (npgsqlStrategy == NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
            sb.Append(" GENERATED ALWAYS AS IDENTITY");
      }
   }
}
