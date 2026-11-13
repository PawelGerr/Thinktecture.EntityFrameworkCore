using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates MySQL temporary tables.
/// </summary>
public sealed class MySqlTempTableCreator : ITempTableCreator
{
   private readonly DbContext _context;
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlTempTableCreator" />.
   /// </summary>
   /// <param name="context">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   public MySqlTempTableCreator(
      ICurrentDbContext context,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource)
   {
      ArgumentNullException.ThrowIfNull(context);
      _context = context.Context ?? throw new ArgumentNullException(nameof(context));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public async Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(options);

      if (entityType.IsOwned())
         throw new NotSupportedException("MySQL temporary tables do not support owned entities.");

      var nameLease = options.TableNameProvider.LeaseName(_context, entityType);

      try
      {
         var sql = CreateStatement(entityType, nameLease.Name, options);
         await _context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            if (options.TruncateTableIfExists)
            {
               var dropSql = String.Concat("DROP TEMPORARY TABLE IF EXISTS ", _sqlGenerationHelper.DelimitIdentifier(nameLease.Name), ";");
               await _context.Database.ExecuteSqlRawAsync(dropSql, cancellationToken).ConfigureAwait(false);
            }

            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
         }
         catch
         {
            await _context.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
         }

         return new MySqlTempTableReference(_context.Database, _logger, _sqlGenerationHelper, nameLease.Name, nameLease, options.DropTableOnDispose);
      }
      catch
      {
         nameLease.Dispose();
         throw;
      }
   }

   private string CreateStatement(IEntityType entityType, string tableName, ITempTableCreationOptions options)
   {
      var properties = options.PropertiesToInclude.DeterminePropertiesForTempTable(entityType);
      var primaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, properties);
      var definitions = new StringBuilder();
      var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
      var isTempTableEntity = entityType.ClrType != typeof(object) && entityType.Name == EntityNameProvider.GetTempTableName(entityType.ClrType);
      StoreObjectIdentifier? storeObject = null;

      for (var i = 0; i < properties.Count; i++)
      {
         var property = properties[i];
         storeObject ??= property.GetStoreObject();

         if (i != 0)
            definitions.AppendLine(",");

         var columnName = property.GetColumnName(storeObject.Value)
                          ?? throw new InvalidOperationException($"The property '{property.Name}' has no column name.");
         var columnType = property.GetColumnType(storeObject.Value);

         definitions.Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(' ')
                    .Append(columnType);

         // A differing collation/charset between a temp table and a regular table fails joins with error 1267.
         // The charset and collation exist in the design-time model only.
         // The store type decides, not the CLR type: a value-converted enum is a varchar, a string mapped to json has no charset.
         var designTimeProperty = SupportsCharSet(columnType)
                                     ? designTimeModel.FindEntityType(entityType.Name)?.FindProperty(property.Name)
                                     : null;

         if (designTimeProperty is not null)
         {
            var charSet = designTimeProperty.GetCharSet() ?? designTimeModel.GetCharSet();
            var collation = designTimeProperty.GetCollation() ?? designTimeModel.GetCollation();

            if (!String.IsNullOrEmpty(charSet))
               definitions.Append(" CHARACTER SET ").Append(charSet);

            if (!String.IsNullOrEmpty(collation))
               definitions.Append(" COLLATE ").Append(collation);
         }

         definitions.Append(property.IsNullable ? " NULL" : " NOT NULL");

         // Only registered temp-table entities generate values; a staging copy of a regular entity must keep explicit ids (0 would be renumbered).
         if (isTempTableEntity && property.GetValueGenerationStrategy() == MySqlValueGenerationStrategy.IdentityColumn)
         {
            if (!primaryKeys.Contains(property))
               throw new NotSupportedException($"MySQL requires auto-increment property '{property.Name}' to be part of the temporary table primary key.");

            definitions.Append(" AUTO_INCREMENT");
         }

         var defaultValueSql = property.GetDefaultValueSql(storeObject.Value);

         if (!String.IsNullOrWhiteSpace(defaultValueSql))
         {
            definitions.Append(" DEFAULT ").Append(defaultValueSql);
         }
         else if (property.TryGetDefaultValue(storeObject.Value, out var defaultValue) && defaultValue is not null)
         {
            var converter = property.GetValueConverter();
            if (converter is not null)
               defaultValue = converter.ConvertToProvider(defaultValue);

            if (defaultValue is not null)
            {
               var mapping = _typeMappingSource.FindMapping(defaultValue.GetType(), columnType)
                             ?? _typeMappingSource.GetMappingForValue(defaultValue);
               definitions.Append(" DEFAULT ").Append(mapping.GenerateSqlLiteral(defaultValue));
            }
         }
      }

      if (primaryKeys.Count != 0)
      {
         if (definitions.Length != 0)
            definitions.AppendLine(",");

         definitions.Append("PRIMARY KEY (")
                    .Append(String.Join(", ", primaryKeys.Select(property =>
                    {
                       var propertyStoreObject = property.GetStoreObject();
                       var columnName = property.GetColumnName(propertyStoreObject)
                                        ?? throw new InvalidOperationException($"The property '{property.Name}' has no column name.");
                       return _sqlGenerationHelper.DelimitIdentifier(columnName);
                    })))
                    .Append(')');
      }

      var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(tableName);
      var createTable = $"CREATE TEMPORARY TABLE {tableIdentifier} ({definitions});";

      return createTable;
   }

   // MySQL accepts CHARACTER SET / COLLATE for character and enumeration columns only (not for json, blob, etc.).
   private static bool SupportsCharSet(string columnType)
   {
      var end = columnType.IndexOfAny(['(', ' ']);
      var baseType = end < 0 ? columnType : columnType[..end];

      return baseType.ToLowerInvariant() is "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" or "enum" or "set";
   }
}

