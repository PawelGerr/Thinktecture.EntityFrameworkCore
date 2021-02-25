using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk insert or update.
   /// </summary>
   public interface IBulkInsertOrUpdateExecutor
   {
      /// <summary>
      /// Creates options with default values.
      /// </summary>
      /// <param name="propertiesToInsert">Provides properties to insert.</param>
      /// <param name="propertiesToUpdate">Provides properties to update.</param>
      /// <param name="keyProperties">Provides key properties.</param>
      /// <returns>Options to use with <see cref="IBulkInsertExecutor"/>.</returns>
      IBulkInsertOrUpdateOptions CreateOptions(
         IEntityPropertiesProvider? propertiesToInsert = null,
         IEntityPropertiesProvider? propertiesToUpdate = null,
         IEntityPropertiesProvider? keyProperties = null);

      /// <summary>
      /// Performs bulk insert or update.
      /// </summary>
      /// <param name="entities">Entities to insert or update.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entities to insert or update.</typeparam>
      /// <returns>Number of affected rows.</returns>
      Task<int> BulkInsertOrUpdateAsync<T>(
         IEnumerable<T> entities,
         IBulkInsertOrUpdateOptions options,
         CancellationToken cancellationToken = default)
         where T : class;
   }
}
