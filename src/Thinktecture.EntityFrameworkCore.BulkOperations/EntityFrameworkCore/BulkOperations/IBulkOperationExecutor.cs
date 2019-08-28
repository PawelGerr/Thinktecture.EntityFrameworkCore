using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   public interface IBulkOperationExecutor
   {
      /// <summary>
      /// Creates options with default values.
      /// </summary>
      /// <returns>Options to use with <see cref="IBulkOperationExecutor"/>.</returns>
      [NotNull]
      IBulkInsertOptions CreateOptions();

      /// <summary>
      /// Performs bulk insert.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      /// <returns></returns>
      [NotNull]
      Task BulkInsertAsync<T>([NotNull] DbContext ctx,
                              [NotNull] IEntityType entityType,
                              [NotNull] IEnumerable<T> entities,
                              [NotNull] IBulkInsertOptions options,
                              CancellationToken cancellationToken = default)
         where T : class;

      /// <summary>
      /// Performs bulk insert on table with the name <paramref name="tableName"/>.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="schema">Schema of the table.</param>
      /// <param name="tableName">Name of the table to insert into.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      /// <returns></returns>
      [NotNull]
      Task BulkInsertAsync<T>([NotNull] DbContext ctx,
                              [NotNull] IEntityType entityType,
                              [NotNull] IEnumerable<T> entities,
                              [CanBeNull] string schema,
                              [NotNull] string tableName,
                              [NotNull] IBulkInsertOptions options,
                              CancellationToken cancellationToken = default)
         where T : class;
   }
}
