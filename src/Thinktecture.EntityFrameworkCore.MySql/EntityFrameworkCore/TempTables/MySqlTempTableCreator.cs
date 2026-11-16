using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Storage;
using Microting.EntityFrameworkCore.MySql.Storage.Internal;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;
using Thinktecture.EntityFrameworkCore.Internal;
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
         EnsureNoModelTableIsHidden(nameLease.Name);
         await _context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            if (options.TruncateTableIfExists)
            {
               var dropSql = String.Concat("DROP TEMPORARY TABLE IF EXISTS ", _sqlGenerationHelper.DelimitIdentifier(nameLease.Name), ";");
               await ExecuteSqlAsync(dropSql, cancellationToken).ConfigureAwait(false);
            }

            await ExecuteSqlAsync(sql, cancellationToken).ConfigureAwait(false);
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

   // IDefaultValueCompatibilityAware of the provider is the only way to get a date or time literal that MySQL accepts as a column default.
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private string CreateStatement(IEntityType entityType, string tableName, ITempTableCreationOptions options)
   {
      var properties = options.PropertiesToInclude.DeterminePropertiesForTempTable(entityType);
      var primaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, properties);
      var definitions = new StringBuilder();
      IModel? designTimeModel = null;
      var isTempTableEntity = IsTempTableEntity(entityType);
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
         // Only a character column needs the design-time model, which is expensive to build with a compiled model.
         if (MySqlColumnCharSet.SupportsCharSet(columnType))
         {
            designTimeModel ??= _context.GetService<IDesignTimeModel>().Model;
            definitions.Append(MySqlColumnCharSet.GetColumnClauses(designTimeModel, storeObject.Value, columnName, columnType));
         }

         definitions.Append(property.IsColumnNullable(storeObject.Value) ? " NULL" : " NOT NULL");

         // Only registered temp-table entities generate values; a temp table of a regular entity must keep explicit ids (0 would be renumbered).
         if (isTempTableEntity
             && property.GetValueGenerationStrategy() == MySqlValueGenerationStrategy.IdentityColumn
             && MySqlPropertyExtensions.IsCompatibleAutoIncrementColumn(property))
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

               // Date and time literals have the form "TIMESTAMP '...'", which MySQL rejects as a column default. The migrations of the provider use the same clone.
               if (mapping is IDefaultValueCompatibilityAware defaultValueCompatibleMapping)
                  mapping = defaultValueCompatibleMapping.Clone(isDefaultValueCompatible: true);

               // MySQL accepts a default of a TEXT, BLOB or JSON column as an expression only (error 1101 otherwise).
               var isExpressionDefault = IsTextBlobOrJson(columnType);

               // The provider writes a line break as CONCAT('a', CHAR(10), 'b'), which MySQL accepts as a default only in parentheses (error 1064).
               // A quoted literal may contain the line break itself.
               if (!isExpressionDefault && mapping is MySqlStringTypeMapping stringMapping)
                  mapping = stringMapping.Clone(replaceLineBreaksWithCharFunction: false);

               var literal = mapping.WithFractionalSecondsPrecision().GenerateSqlLiteral(defaultValue);
               definitions.Append(isExpressionDefault ? $" DEFAULT ({literal})" : $" DEFAULT {literal}");
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
      var tableClauses = designTimeModel is null ? String.Empty : MySqlColumnCharSet.GetTableClauses(designTimeModel, storeObject!.Value);
      var createTable = $"CREATE TEMPORARY TABLE {tableIdentifier} ({definitions}){tableClauses};";

      return createTable;
   }

   // A MySQL temporary table hides the base table with the same name for the connection, so queries and SaveChanges would use the temporary table.
   private void EnsureNoModelTableIsHidden(string tempTableName)
   {
      var identifier = _sqlGenerationHelper.DelimitIdentifier(tempTableName);
      var hidesModelTable = _context.Model.GetRelationalModel().Tables
                                    .Any(table => String.Equals(_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema), identifier, StringComparison.OrdinalIgnoreCase)
                                                  && table.EntityTypeMappings.Any(mapping => !IsTempTableEntity(mapping.TypeBase.ContainingEntityType)));

      if (hidesModelTable)
         throw new NotSupportedException($"The temporary table name '{tempTableName}' is the name of a table of the model. A MySQL temporary table hides the table with the same name for the connection, so queries and 'SaveChanges' would use the temporary table. Use an '{nameof(ITempTableNameProvider)}' that returns a name that is not a table of the model.");
   }

   private static bool IsTempTableEntity(IEntityType entityType)
   {
      return entityType.ClrType != typeof(object) && entityType.Name == EntityNameProvider.GetTempTableName(entityType.ClrType);
   }

   // "Build(sql)" does not format the text, so braces in a JSON default like '{}' are kept; ExecuteSqlRawAsync would apply String.Format.
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private Task<int> ExecuteSqlAsync(string statement, CancellationToken cancellationToken)
   {
      var command = _context.GetService<IRawSqlCommandBuilder>().Build(statement);
      return new EfCoreValueExpressionTranslator(_context).ExecuteNonQueryAsync(command, ReadOnlyDictionary<string, object?>.Empty, cancellationToken);
   }

   /// <summary>
   /// Indicates whether the column type is a TEXT, BLOB or JSON type. MySQL cannot index such a column without a prefix length
   /// and accepts its default value as an expression only.
   /// </summary>
   internal static bool IsTextBlobOrJson(string columnType)
   {
      return GetBaseType(columnType) is "tinytext" or "text" or "mediumtext" or "longtext" or "tinyblob" or "blob" or "mediumblob" or "longblob" or "json";
   }

   /// <summary>
   /// Returns the type name of a column type without length, values and attributes in lower case, for example <c>varchar</c> for <c>VARCHAR(100)</c>.
   /// </summary>
   internal static string GetBaseType(string columnType)
   {
      var end = columnType.IndexOfAny(['(', ' ']);
      var baseType = end < 0 ? columnType : columnType[..end];

      return baseType.ToLowerInvariant();
   }
}

