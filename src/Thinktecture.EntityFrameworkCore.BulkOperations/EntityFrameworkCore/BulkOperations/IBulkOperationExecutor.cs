using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
      IBulkInsertOptions CreateOptions();

      /// <summary>
      /// Performs bulk insert.
      /// </summary>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      /// <returns></returns>
      Task BulkInsertAsync<T>(
         IEnumerable<T> entities,
         IBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class;
   }
}
