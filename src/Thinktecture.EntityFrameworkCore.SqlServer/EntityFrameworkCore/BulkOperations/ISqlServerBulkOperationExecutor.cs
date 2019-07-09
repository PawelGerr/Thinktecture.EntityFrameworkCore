using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   public interface ISqlServerBulkOperationExecutor
   {
      /// <summary>
      /// Performs bulk insert.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      /// <returns></returns>
      [NotNull]
      Task BulkInsertAsync<T>([NotNull] DbContext ctx,
                              [NotNull] IEnumerable<T> entities,
                              [NotNull] SqlBulkInsertOptions options,
                              CancellationToken cancellationToken = default)
         where T : class;

      /// <summary>
      /// Performs bulk insert on table with the name <paramref name="tableName"/>.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="schema">Schema of the table.</param>
      /// <param name="tableName">Name of the table to insert into.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      /// <returns></returns>
      [NotNull]
      Task BulkInsertAsync<T>([NotNull] DbContext ctx,
                              [NotNull] IEnumerable<T> entities,
                              [CanBeNull] string schema,
                              [NotNull] string tableName,
                              [NotNull] SqlBulkInsertOptions options,
                              CancellationToken cancellationToken = default)
         where T : class;
   }
}
