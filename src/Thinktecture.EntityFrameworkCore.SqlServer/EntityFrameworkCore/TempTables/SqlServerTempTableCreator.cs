using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
   public class SqlServerTempTableCreator : ITempTableCreator
   {
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <summary>
      /// Initializes <see cref="SqlServerTempTableCreator"/>.
      /// </summary>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      public SqlServerTempTableCreator([NotNull] ISqlGenerationHelper sqlGenerationHelper,
                                       [NotNull] IRelationalTypeMappingSource typeMappingSource)
      {
         _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      }

      /// <inheritdoc />
      public async Task<ITempTableReference> CreateTempTableAsync(DbContext ctx,
                                                                  IEntityType entityType,
                                                                  TempTableCreationOptions options,
                                                                  CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var tableName = GetTableName(entityType, options.MakeTableNameUnique);
         var properties = entityType.GetProperties();
         var sql = GetTempTableCreationSql(properties, tableName, options.MakeTableNameUnique);

         await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
#pragma warning disable EF1000
            await ctx.Database.ExecuteSqlCommandAsync(sql, cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1000
         }
         catch (Exception)
         {
            ctx.Database.CloseConnection();
            throw;
         }

         var logger = ctx.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

         return new SqlServerTempTableReference(logger, _sqlGenerationHelper, tableName, ctx.Database);
      }

      [NotNull]
      private static string GetTableName([NotNull] IEntityType entityType, bool makeTableNameUnique)
      {
         var tableName = entityType.Relational().TableName;

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         if (makeTableNameUnique)
            tableName = $"{tableName}_{Guid.NewGuid():N}";

         return tableName;
      }

      /// <inheritdoc />
      public async Task CreatePrimaryKeyAsync(DbContext ctx,
                                              IEntityType entityType,
                                              string tableName,
                                              bool checkForExistence = false,
                                              CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         IEnumerable<string> columnNames;

         if (entityType.IsQueryType)
         {
            columnNames = entityType.GetProperties().Select(p => p.Relational().ColumnName);
         }
         else
         {
            var pk = entityType.FindPrimaryKey();
            columnNames = pk.Properties.Select(p => p.Relational().ColumnName);
         }

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
#pragma warning disable EF1000
         await ctx.Database.ExecuteSqlCommandAsync(sql, cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1000
      }

      [NotNull]
      private string GetTempTableCreationSql([NotNull] IEnumerable<IProperty> properties, [NotNull] string tableName, bool isUnique)
      {
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var sql = $@"
      CREATE TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName)}
      (
{GetColumnsDefinitions(properties)}
      );";

         if (isUnique)
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

      [NotNull]
      private string GetColumnsDefinitions([NotNull] IEnumerable<IProperty> properties)
      {
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         var sb = new StringBuilder();
         var isFirst = true;

         foreach (var property in properties)
         {
            if (!isFirst)
               sb.AppendLine(",");

            var relational = property.Relational();

            sb.Append("\t\t")
              .Append(_sqlGenerationHelper.DelimitIdentifier(relational.ColumnName)).Append(" ")
              .Append(relational.ColumnType)
              .Append(property.IsNullable ? " NULL" : " NOT NULL");

            if (IsIdentityColumn(property))
               sb.Append(" IDENTITY");

            if (!String.IsNullOrWhiteSpace(relational.DefaultValueSql))
            {
               sb.Append(" DEFAULT (").Append(relational.DefaultValueSql).Append(")");
            }
            else if (relational.DefaultValue != null && relational.DefaultValue != DBNull.Value)
            {
               var mappingForValue = _typeMappingSource.GetMappingForValue(relational.DefaultValue);
               sb.Append(" DEFAULT ").Append(mappingForValue.GenerateSqlLiteral(relational.DefaultValue));
            }

            isFirst = false;
         }

         return sb.ToString();
      }

      private static bool IsIdentityColumn([NotNull] IProperty property)
      {
         return SqlServerValueGenerationStrategy.IdentityColumn.Equals(property.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy)?.Value);
      }
   }
}
