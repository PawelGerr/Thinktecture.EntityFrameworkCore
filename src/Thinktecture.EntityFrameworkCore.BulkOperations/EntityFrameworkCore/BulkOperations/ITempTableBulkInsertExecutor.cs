using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

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

   /// <summary>
   /// Inserts the provided <paramref name="values"/> into a temp table.
   /// </summary>
   /// <param name="values">Values to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the values.</typeparam>
   /// <returns>A query returning the inserted <paramref name="values"/>.</returns>
   Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
      IEnumerable<TColumn1> values,
      ITempTableBulkInsertOptions options,
      CancellationToken cancellationToken);
}
