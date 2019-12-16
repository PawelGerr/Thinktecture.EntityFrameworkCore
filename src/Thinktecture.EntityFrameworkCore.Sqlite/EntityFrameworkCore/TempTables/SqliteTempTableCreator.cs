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
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <summary>
      /// Initializes <see cref="SqliteTempTableCreator"/>.
      /// </summary>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      public SqliteTempTableCreator(ISqlGenerationHelper sqlGenerationHelper,
                                    IRelationalTypeMappingSource typeMappingSource)
      {
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      }

      /// <inheritdoc />
      public async Task<ITempTableReference> CreateTempTableAsync(DbContext ctx,
                                                                  IEntityType entityType,
                                                                  ITempTableCreationOptions options,
                                                                  CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var tableName = GetTableName(ctx, entityType, options.TableNameProvider);
         var sql = GetTempTableCreationSql(entityType, tableName, options);

         await ctx.Database.OpenConnectionAsync(cancellationToken);

         try
         {
            await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken);
         }
         catch (Exception)
         {
            ctx.Database.CloseConnection();
            throw;
         }

         var logger = ctx.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

         return new SqliteTempTableReference(logger, _sqlGenerationHelper, tableName, ctx.Database);
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

         if (!options.DropTempTableIfExists)
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
              .Append(_sqlGenerationHelper.DelimitIdentifier(property.GetColumnName())).Append(" ")
              .Append(property.GetColumnType())
              .Append(property.IsNullable ? " NULL" : " NOT NULL");

            if (property.IsAutoIncrement())
               sb.Append("AUTOINCREMENT");

            var defaultValueSql = property.GetDefaultValueSql();

            if (!String.IsNullOrWhiteSpace(defaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(defaultValueSql).Append(")");
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

      private static void CreatePkClause(IEntityType entityType, IReadOnlyList<IProperty> properties, StringBuilder sb)
      {
         var keyProperties = entityType.FindPrimaryKey()?.Properties ?? entityType.GetProperties();

         if (keyProperties.Any())
         {
            var missingColumns = keyProperties.Except(properties);

            if (missingColumns.Any())
               throw new ArgumentException($"Cannot create PRIMARY KEY because not all key columns are part of the temp table. Missing columns: {String.Join(", ", missingColumns.Select(c => c.GetColumnName()))}.");

            var columnNames = keyProperties.Select(p => p.GetColumnName());

            sb.AppendLine(",");
            sb.Append("\t\tPRIMARY KEY (");
            var isFirst = true;

            foreach (var columnName in columnNames)
            {
               if (!isFirst)
                  sb.Append(", ");

               sb.Append(columnName);
               isFirst = false;
            }

            sb.Append(")");
         }
      }

      private static string GetTableName(DbContext ctx, IEntityType entityType, ITempTableNameProvider nameProvider)
      {
         if (nameProvider == null)
            throw new ArgumentNullException(nameof(nameProvider));

         var tableName = nameProvider.GetName(ctx, entityType);

         return tableName;
      }
   }
}
