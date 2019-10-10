using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class SqliteDbContextExtensions
   {
      /// <summary>
      /// Copies <paramref name="entities"/> into a table.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      public static Task BulkInsertAsync<T>(this DbContext ctx,
                                            IEnumerable<T> entities,
                                            SqliteBulkInsertOptions? options,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         return ctx.BulkInsertAsync(entities, (IBulkInsertOptions?)options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="values">Values to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
      public static Task<ITempTableQuery<TempTable<TColumn1>>> BulkInsertValuesIntoTempTableAsync<TColumn1>(this DbContext ctx,
                                                                                                            IEnumerable<TColumn1> values,
                                                                                                            SqliteTempTableBulkInsertOptions? options = null,
                                                                                                            CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(v => new TempTable<TColumn1>(v));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="values"/> into a temp table
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
      public static Task<ITempTableQuery<TempTable<TColumn1, TColumn2>>> BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(this DbContext ctx,
                                                                                                                                IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
                                                                                                                                SqliteTempTableBulkInsertOptions? options = null,
                                                                                                                                CancellationToken cancellationToken = default)
      {
         if (values == null)
            throw new ArgumentNullException(nameof(values));

         var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

         return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="entities"/> into a temp table
      /// and returns the query for accessing the inserted records.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <returns>A query for accessing the inserted values.</returns>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      public static async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(this DbContext ctx,
                                                                                   IEnumerable<T> entities,
                                                                                   SqliteTempTableBulkInsertOptions? options = null,
                                                                                   CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));

         options ??= new SqliteTempTableBulkInsertOptions();
         var entityType = ctx.Model.GetEntityType(typeof(T));
         var tempTableCreator = ctx.GetService<ITempTableCreator>();
         var bulkInsertExecutor = ctx.GetService<IBulkOperationExecutor>();

         var tempTableReference = await tempTableCreator.CreateTempTableAsync(ctx, entityType, options.TempTableCreationOptions, cancellationToken).ConfigureAwait(false);

         try
         {
            await bulkInsertExecutor.BulkInsertAsync(ctx, entityType, entities, null, tempTableReference.Name, options.BulkInsertOptions, cancellationToken).ConfigureAwait(false);

            var query = ctx.GetTempTableQuery<T>(tempTableReference.Name);

            return new TempTableQuery<T>(query, tempTableReference);
         }
         catch (Exception)
         {
            tempTableReference.Dispose();
            throw;
         }
      }

      private static IQueryable<T> GetTempTableQuery<T>(this DbContext ctx, string tableName)
         where T : class
      {
         var sqlHelper = ctx.GetService<ISqlGenerationHelper>();

         return ctx.Set<T>().FromSqlRaw($"SELECT * FROM {sqlHelper.DelimitIdentifier(tableName)}");
      }
   }
}
