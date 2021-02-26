using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Inserts entities into a temp table.
   /// </summary>
   public interface ITempTableBulkInsertExecutor
   {
      /// <summary>
      /// Creates options with default values.
      /// </summary>
      /// <param name="propertiesToInsert">Properties to insert.</param>
      /// <returns>Options to use with <see cref="ITempTableBulkInsertExecutor"/>.</returns>
      ITempTableBulkInsertOptions CreateOptions(IEntityPropertiesProvider? propertiesToInsert = null);

      /// <summary>
      /// Inserts the provided <paramref name="entities"/> into a temp table.
      /// </summary>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entities.</typeparam>
      /// <returns>A query returning the inserted <paramref name="entities"/>.</returns>
      Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
         IEnumerable<T> entities,
         ITempTableBulkInsertOptions options,
         CancellationToken cancellationToken = default)
         where T : class;
   }
}
