using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      private static readonly RowVersionValueConverter _rowVersionConverter;

      static DbContextExtensions()
      {
         _rowVersionConverter = new RowVersionValueConverter();
      }

      /// <summary>
      /// Fetches MIN_ACTIVE_ROWVERSION from SQL Server.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>The result of MIN_ACTIVE_ROWVERSION call.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task<ulong> GetMinActiveRowVersionAsync([NotNull] this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         using (var command = ctx.Database.GetDbConnection().CreateCommand())
         {
            command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();

            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (ulong)_rowVersionConverter.ConvertFromProvider(bytes);
         }
      }

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task CreateTempTableAsync<TColumn1>([NotNull] this DbContext ctx)
      {
         await CreateTempTableAsync(ctx, typeof(TempTable<TColumn1>)).ConfigureAwait(false);
      }

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task CreateTempTableAsync<TColumn1, TColumn2>([NotNull] this DbContext ctx)
      {
         await CreateTempTableAsync(ctx, typeof(TempTable<TColumn1, TColumn2>)).ConfigureAwait(false);
      }

      private static async Task CreateTempTableAsync([NotNull] DbContext ctx, [NotNull] Type type)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var sql = GetTempTableCreationSql(ctx, type);

         await ctx.Database.ExecuteSqlCommandAsync(sql).ConfigureAwait(false);
      }

      [NotNull]
      private static string GetTempTableCreationSql([NotNull] DbContext ctx, [NotNull] Type type)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var (_, tableName) = ctx.GetTableIdentifier(type);

         var sql = $@"
IF(OBJECT_ID('tempdb..{tableName}') IS NOT NULL)
      TRUNCATE TABLE [{tableName}];
ELSE
BEGIN
      CREATE TABLE [{tableName}]
      (
          {GetColumnsDefinitions(ctx, type)}
      );
END
";

         return sql;
      }

      [NotNull]
      private static string GetColumnsDefinitions([NotNull] DbContext ctx, [NotNull] Type type)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var entityType = ctx.Model.FindEntityType(type);

         if (entityType == null)
            throw new ArgumentException($"The provided type '{type.DisplayName()}' is not known by the database context '{ctx.GetType().DisplayName()}'.", nameof(type));

         var sb = new StringBuilder();
         var isFirst = true;

         foreach (var property in entityType.GetProperties())
         {
            if (!isFirst)
               sb.AppendLine(",");

            var relational = property.Relational();

            sb.Append(relational.ColumnName).Append(" ")
              .Append(relational.ColumnType).Append(" ")
              .Append(property.IsNullable ? "NULL" : "NOT NULL");

            isFirst = false;
         }

         return sb.AppendLine().ToString();
      }
   }
}
