using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables.
   /// </summary>
   public class SqlServerTempTableCreator : ITempTableCreator
   {
      /// <inheritdoc />
      public Task<string> CreateTempTableAsync<T>(DbContext ctx, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
         where T : class
      {
         return CreateTempTableAsync(ctx, typeof(T), makeTableNameUnique, cancellationToken);
      }

      /// <inheritdoc />
      public async Task<string> CreateTempTableAsync(DbContext ctx, Type type, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var (_, tableName) = ctx.GetTableIdentifier(type);

         if (!tableName.StartsWith("#", StringComparison.Ordinal))
            tableName = $"#{tableName}";

         if (makeTableNameUnique)
            tableName = $"{tableName}_{Guid.NewGuid():N}";

         var sql = GetTempTableCreationSql(ctx, type, tableName, makeTableNameUnique);

         await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable EF1000
         await ctx.Database.ExecuteSqlCommandAsync(sql, cancellationToken).ConfigureAwait(false);
#pragma warning restore EF1000

         return tableName;
      }

      /// <inheritdoc />
      public async Task CreatePrimaryKeyAsync<T>(DbContext ctx, string tableName, bool checkForExistence = false, CancellationToken cancellationToken = default)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         var entityType = ctx.GetEntityType<T>();
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
ALTER TABLE [{tableName}]
ADD CONSTRAINT [PK_{tableName}_{Guid.NewGuid():N}] PRIMARY KEY CLUSTERED ({String.Join(", ", columnNames)});
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
      private static string GetTempTableCreationSql([NotNull] DbContext ctx, [NotNull] Type type, [NotNull] string tableName, bool isUnique)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var sql = $@"
      CREATE TABLE [{tableName}]
      (
{GetColumnsDefinitions(ctx, type)}
      );";

         if (isUnique)
            return sql;

         return $@"
IF(OBJECT_ID('tempdb..{tableName}') IS NOT NULL)
      TRUNCATE TABLE [{tableName}];
ELSE
BEGIN
{sql}
END
";
      }

      [NotNull]
      private static string GetColumnsDefinitions([NotNull] DbContext ctx, [NotNull] Type type)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var entityType = ctx.GetEntityType(type);
         var sb = new StringBuilder();
         var isFirst = true;

         foreach (var property in entityType.GetProperties())
         {
            if (!isFirst)
               sb.AppendLine(",");

            var relational = property.Relational();

            sb.Append("\t\t")
              .Append(relational.ColumnName).Append(" ")
              .Append(relational.ColumnType).Append(" ")
              .Append(property.IsNullable ? "NULL" : "NOT NULL");

            isFirst = false;
         }

         return sb.ToString();
      }
   }
}
