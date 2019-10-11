using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class BulkOperationsDbContextExtensions
   {
      /// <summary>
      /// Creates a temp table using custom type '<typeparamref name="T"/>'.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of custom temp table.</typeparam>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by the provided <paramref name="ctx"/>.</exception>
      public static Task<ITempTableReference> CreateTempTableAsync<T>(this DbContext ctx,
                                                                      bool makeTableNameUnique = true,
                                                                      CancellationToken cancellationToken = default)
         where T : class
      {
         return ctx.CreateTempTableAsync(typeof(T), new TempTableCreationOptions { MakeTableNameUnique = makeTableNameUnique }, cancellationToken);
      }

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="type">Type of the entity.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or  <paramref name="type"/> is <c>null</c>
      /// - or  <paramref name="options"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
      public static Task<ITempTableReference> CreateTempTableAsync(this DbContext ctx,
                                                                   Type type,
                                                                   TempTableCreationOptions options,
                                                                   CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         var entityType = ctx.Model.GetEntityType(type);
         return ctx.GetService<ITempTableCreator>().CreateTempTableAsync(ctx, entityType, options, cancellationToken);
      }

      /// <summary>
      /// Copies <paramref name="entities"/> into a table.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="propertiesToInsert">Properties to insert.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      public static Task BulkInsertAsync<T>(this DbContext ctx,
                                            IEnumerable<T> entities,
                                            Expression<Func<T, object>> propertiesToInsert,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         var bulkInsertExecutor = ctx.GetService<IBulkOperationExecutor>();

         var options = bulkInsertExecutor.CreateOptions();
         options.EntityMembersProvider = EntityMembersProvider.From(propertiesToInsert);

         return BulkInsertAsync(bulkInsertExecutor, ctx, entities, options, cancellationToken);
      }

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
                                            IBulkInsertOptions? options = null,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         var bulkInsertExecutor = ctx.GetService<IBulkOperationExecutor>();
         options ??= bulkInsertExecutor.CreateOptions();

         return BulkInsertAsync(bulkInsertExecutor, ctx, entities, options, cancellationToken);
      }

      private static async Task BulkInsertAsync<T>(IBulkOperationExecutor bulkInsertExecutor,
                                                   DbContext ctx,
                                                   IEnumerable<T> entities,
                                                   IBulkInsertOptions options,
                                                   CancellationToken cancellationToken)
         where T : class
      {
         if (bulkInsertExecutor == null)
            throw new ArgumentNullException(nameof(bulkInsertExecutor));
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         var entityType = ctx.Model.GetEntityType(typeof(T));

         await bulkInsertExecutor.BulkInsertAsync(ctx, entityType, entities, options, cancellationToken).ConfigureAwait(false);
      }
   }
}
