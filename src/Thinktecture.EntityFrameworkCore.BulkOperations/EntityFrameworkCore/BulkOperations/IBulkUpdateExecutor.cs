using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   public interface IBulkUpdateExecutor
   {
      /// <summary>
      /// Creates options with default values.
      /// </summary>
      /// <returns>Options to use with <see cref="IBulkInsertExecutor"/>.</returns>
      IBulkUpdateOptions CreateOptions();

      /// <summary>
      /// Performs bulk update.
      /// </summary>
      /// <param name="entities">Entities to update.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entities to update.</typeparam>
      /// <returns>Number of affected rows.</returns>
      Task<int> BulkUpdateAsync<T>(
         IEnumerable<T> entities,
         IBulkUpdateOptions options,
         CancellationToken cancellationToken = default)
         where T : class;
   }
}
