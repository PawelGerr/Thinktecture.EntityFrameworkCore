using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Inserts entities into a temp table.
   /// </summary>
   public interface ITempTableBulkOperationExecutor
   {
      /// <summary>
      /// Creates options with default values.
      /// </summary>
      /// <returns>Options to use with <see cref="IBulkOperationExecutor"/>.</returns>
      ITempTableBulkInsertOptions CreateOptions();

      /// <summary>
      /// Inserts the provided <paramref name="entities"/> into a temp table.
      /// </summary>
      /// <param name="ctx">The database context to use.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entities.</typeparam>
      /// <returns>A query returning the inserted <paramref name="entities"/>.</returns>
      Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(DbContext ctx,
                                                               IEnumerable<T> entities,
                                                               ITempTableBulkInsertOptions options,
                                                               CancellationToken cancellationToken = default)
         where T : class;
   }
}
