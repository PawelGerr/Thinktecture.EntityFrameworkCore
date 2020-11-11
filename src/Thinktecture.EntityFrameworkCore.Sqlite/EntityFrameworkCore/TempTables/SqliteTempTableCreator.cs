using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables.
   /// </summary>
   public sealed class SqliteTempTableCreator : ITempTableCreator
   {
      private readonly DbContext _ctx;
      private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <summary>
      /// Initializes <see cref="SqliteTempTableCreator"/>.
      /// </summary>
      /// <param name="ctx">Current database context.</param>
      /// <param name="logger">Logger.</param>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      public SqliteTempTableCreator(
         ICurrentDbContext ctx,
         IDiagnosticsLogger<DbLoggerCategory.Query> logger,
         ISqlGenerationHelper sqlGenerationHelper,
         IRelationalTypeMappingSource typeMappingSource)
      {
         _ctx = ctx?.Context ?? throw new ArgumentNullException(nameof(ctx));
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

         return new SqliteTempTableReference(_logger, _sqlGenerationHelper, tableName, _ctx.Database, nameLease, options.DropTableOnDispose);
      }

      private string GetTempTableCreationSql(IEntityType entityType, string tableName, ITempTableCreationOptions options)
      {
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var sql = $@"
      CREATE TEMPORARY TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName)}
      (
{GetColumnsDefinitions(entityType, options)}
      );";

         if (!options.TruncateTableIfExists)
            return sql;

         return $@"
DROP TABLE IF EXISTS {_sqlGenerationHelper.DelimitIdentifier(tableName, "temp")};

{sql}
";
      }

      private string GetColumnsDefinitions(IEntityType entityType, ITempTableCreationOptions options)
      {
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

            if (property.IsAutoIncrement())
               sb.Append("AUTOINCREMENT");

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

         if (options.CreatePrimaryKey)
            CreatePkClause(entityType, properties, sb);

         return sb.ToString();
      }

      private void CreatePkClause(IEntityType entityType, IReadOnlyList<IProperty> properties, StringBuilder sb)
      {
         var keyProperties = entityType.FindPrimaryKey()?.Properties ?? entityType.GetProperties();

         if (keyProperties.Any())
         {
            var missingColumns = keyProperties.Except(properties);

            if (missingColumns.Any())
               throw new ArgumentException($"Cannot create PRIMARY KEY because not all key columns are part of the temp table. Missing columns: {String.Join(", ", missingColumns.Select(c => c.GetColumnBaseName()))}.");

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

      private (ITempTableNameLease nameLease, string tableName) GetTableName(
         IEntityType entityType,
         ITempTableNameProvider nameProvider)
      {
         if (nameProvider == null)
            throw new ArgumentNullException(nameof(nameProvider));

         var nameLease = nameProvider.LeaseName(_ctx, entityType);
         var name = nameLease.Name;

         return (nameLease, name);
      }
   }
}
