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
   public class SqlServerTempTableCreator : ITempTableCreator
   {
      private readonly ISqlGenerationHelper _sqlGenerationHelper;
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <summary>
      /// Initializes <see cref="SqlServerTempTableCreator"/>.
      /// </summary>
      /// <param name="sqlGenerationHelper">SQL generation helper.</param>
      /// <param name="typeMappingSource">Type mappings.</param>
      public SqlServerTempTableCreator([JetBrains.Annotations.NotNull] ISqlGenerationHelper sqlGenerationHelper,
                                       [JetBrains.Annotations.NotNull] IRelationalTypeMappingSource typeMappingSource)
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
            await ctx.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
         }
         catch (Exception)
         {
            ctx.Database.CloseConnection();
            throw;
         }

         var logger = ctx.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

         return new SqlServerTempTableReference(logger, _sqlGenerationHelper, tableName, ctx.Database);
      }

      [JetBrains.Annotations.NotNull]
      private static string GetTableName([JetBrains.Annotations.NotNull] IEntityType entityType, bool makeTableNameUnique)
      {
         var tableName = entityType.GetTableName();

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

         var keyProperties = entityType.FindPrimaryKey()?.Properties ?? entityType.GetProperties();
         var columnNames = keyProperties.Select(p => p.GetColumnName());

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

      [JetBrains.Annotations.NotNull]
      private string GetTempTableCreationSql([JetBrains.Annotations.NotNull] IEnumerable<IProperty> properties, [JetBrains.Annotations.NotNull] string tableName, bool isUnique)
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

      [JetBrains.Annotations.NotNull]
      private string GetColumnsDefinitions([JetBrains.Annotations.NotNull] IEnumerable<IProperty> properties)
      {
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

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

            if (IsIdentityColumn(property))
               sb.Append(" IDENTITY");

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

         return sb.ToString();
      }

      private static bool IsIdentityColumn([JetBrains.Annotations.NotNull] IProperty property)
      {
         return SqlServerValueGenerationStrategy.IdentityColumn.Equals(property.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy)?.Value);
      }
   }
}
