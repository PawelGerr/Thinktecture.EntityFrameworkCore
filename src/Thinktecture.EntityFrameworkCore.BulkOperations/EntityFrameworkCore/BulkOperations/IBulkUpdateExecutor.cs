using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Executes bulk updates.
/// </summary>
public interface IBulkUpdateExecutor
{
   /// <summary>
   /// Creates options with default values.
   /// </summary>
   /// <param name="propertiesToUpdate">Provides properties to update.</param>
   /// <param name="keyProperties">Provides key properties.</param>
   /// <returns>Options to use with <see cref="IBulkInsertExecutor"/>.</returns>
   IBulkUpdateOptions CreateOptions(IEntityPropertiesProvider? propertiesToUpdate = null, IEntityPropertiesProvider? keyProperties = null);

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