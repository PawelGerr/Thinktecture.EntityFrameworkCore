using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.BulkOperations;
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
      private static readonly RowVersionValueConverter _rowVersionConverter = new RowVersionValueConverter();

      /// <summary>
      /// Fetches <c>MIN_ACTIVE_ROWVERSION</c> from SQL Server.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>The result of <c>MIN_ACTIVE_ROWVERSION</c> call.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task<long> GetMinActiveRowVersionAsync([NotNull] this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         using (var command = ctx.Database.GetDbConnection().CreateCommand())
         {
            command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();

            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (long)_rowVersionConverter.ConvertFromProvider(bytes);
         }
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="values">Values to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task<IQueryable<TempTable<TColumn1>>> BulkInsertTempTableAsync<TColumn1>([NotNull] this DbContext ctx,
                                                                                             [NotNull] IEnumerable<TColumn1> values,
                                                                                             [CanBeNull] SqlTempTableBulkInsertOptions options = null,
                                                                                             CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(v => new TempTable<TColumn1>(v));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="values">Values to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task<IQueryable<TempTable<TColumn1, TColumn2>>> BulkInsertTempTableAsync<TColumn1, TColumn2>([NotNull] this DbContext ctx,
                                                                                                                 [NotNull] IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
                                                                                                                 [CanBeNull] SqlTempTableBulkInsertOptions options = null,
                                                                                                                 CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="entities"/> into a temp table using <see cref="SqlBulkCopy"/>
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      [NotNull]
      public static async Task<IQueryable<T>> BulkInsertIntoTempTableAsync<T>([NotNull] this DbContext ctx,
                                                                              [NotNull] IEnumerable<T> entities,
                                                                              [CanBeNull] SqlTempTableBulkInsertOptions options = null,
                                                                              CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));

         options = options ?? new SqlTempTableBulkInsertOptions();
         var tableName = await ctx.CreateTempTableAsync<T>(options.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

         if (options.PrimaryKeyCreation == PrimaryKeyCreation.BeforeBulkInsert)
            await ctx.GetService<ITempTableCreator>().CreatePrimaryKeyAsync<T>(ctx, tableName, !options.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

         await ctx.GetService<ISqlServerBulkOperationExecutor>().BulkInsertAsync(ctx, entities, null, tableName, options, cancellationToken).ConfigureAwait(false);

         if (options.PrimaryKeyCreation == PrimaryKeyCreation.AfterBulkInsert)
            await ctx.GetService<ITempTableCreator>().CreatePrimaryKeyAsync<T>(ctx, tableName, !options.MakeTableNameUnique, cancellationToken).ConfigureAwait(false);

         return ctx.GetTempTableQuery<T>(tableName);
      }

      private static IQueryable<T> GetTempTableQuery<T>([NotNull] this DbContext ctx, [NotNull] string tableName)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

         var entityType = ctx.GetEntityType<T>();
         var sql = $"SELECT * FROM [{tableName}]";

#pragma warning disable EF1000
         if (entityType.IsQueryType)
            return ctx.Query<T>().FromSql(sql);

         return ctx.Set<T>().FromSql(sql);
#pragma warning restore EF1000
      }
   }
}
