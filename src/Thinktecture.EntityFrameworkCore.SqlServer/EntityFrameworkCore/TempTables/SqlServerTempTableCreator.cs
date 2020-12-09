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

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqlServerTempTableCreator : ISqlServerTempTableCreator
   {
      private readonly DbContext _ctx;
      private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <summary>
      /// Initializes <see cref="SqlServerTempTableCreator"/>.
      /// </summary>
      /// <param name="ctx">Current database context.</param>
      /// <param name="logger">Logger.</param>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      public SqlServerTempTableCreator(
         ICurrentDbContext ctx,
         IDiagnosticsLogger<DbLoggerCategory.Query> logger,
         ISqlGenerationHelper sqlGenerationHelper,
         IRelationalTypeMappingSource typeMappingSource)
      {
         _ctx = ctx?.Context ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
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
         IReadOnlyCollection<IProperty> keyProperties,
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

         var columnNames = keyProperties.Select(p => p.GetColumnBaseName());

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

      private string GetTempTableCreationSql(IEntityType entityType, string tableName, ITempTableCreationOptions options)
      {
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var sql = $@"
      CREATE TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName)}
      (
{GetColumnsDefinitions(entityType, options)}
      );";

         if (!options.TruncateTableIfExists)
            return sql;

         return $@"
IF(OBJECT_ID('tempdb..{tableName}') IS NOT NULL)
   TRUNCATE TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName)};
ELSE
BEGIN
{sql}
END
";
      }

      private string GetColumnsDefinitions(IEntityType entityType, ITempTableCreationOptions options)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         var properties = options.MembersToInclude.GetPropertiesForTempTable(entityType);
         var sb = new StringBuilder();
         var isFirst = true;

         foreach (var property in properties)
         {
            if (!isFirst)
               sb.AppendLine(",");

            sb.Append("\t\t")
              .Append(_sqlGenerationHelper.DelimitIdentifier(property.GetColumnBaseName())).Append(' ')
              .Append(property.GetColumnType())
              .Append(property.IsNullable ? " NULL" : " NOT NULL");

            if (IsIdentityColumn(property))
               sb.Append(" IDENTITY");

            var defaultValueSql = property.GetDefaultValueSql();

            if (!String.IsNullOrWhiteSpace(defaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(defaultValueSql).Append(')');
            }
            else
            {
               var defaultValue = property.GetDefaultValue();

               if (defaultValue != null && defaultValue != DBNull.Value)
               {
                  var mappingForValue = _typeMappingSource.GetMappingForValue(defaultValue);
                  sb.Append(" DEFAULT ").Append(mappingForValue.GenerateSqlLiteral(defaultValue));
               }
            }

            isFirst = false;
         }

         CreatePkClause(options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, properties), sb);

         return sb.ToString();
      }

      private void CreatePkClause(
         IReadOnlyCollection<IProperty> keyProperties,
         StringBuilder sb)
      {
         if (keyProperties.Count > 0)
         {
            var columnNames = keyProperties.Select(p => p.GetColumnBaseName());

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

      private static bool IsIdentityColumn(IProperty property)
      {
         return SqlServerValueGenerationStrategy.IdentityColumn.Equals(property.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy)?.Value);
      }
   }
}
