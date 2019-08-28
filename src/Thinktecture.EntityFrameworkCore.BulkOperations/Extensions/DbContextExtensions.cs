using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      /// <summary>
      /// Copies <paramref name="entities"/> into a table.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="propertiesToInsert">Properties to insert.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      [NotNull]
      public static Task BulkInsertAsync<T>([NotNull] this DbContext ctx,
                                            [NotNull] IEnumerable<T> entities,
                                            [NotNull] Expression<Func<T, object>> propertiesToInsert,
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
      [NotNull]
      public static Task BulkInsertAsync<T>([NotNull] this DbContext ctx,
                                            [NotNull] IEnumerable<T> entities,
                                            [CanBeNull] IBulkInsertOptions options = null,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         var bulkInsertExecutor = ctx.GetService<IBulkOperationExecutor>();
         options = options ?? bulkInsertExecutor.CreateOptions();

         return BulkInsertAsync(bulkInsertExecutor, ctx, entities, options, cancellationToken);
      }

      private static async Task BulkInsertAsync<T>([NotNull] IBulkOperationExecutor bulkInsertExecutor,
                                                   [NotNull] DbContext ctx,
                                                   [NotNull] IEnumerable<T> entities,
                                                   [NotNull] IBulkInsertOptions options,
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
