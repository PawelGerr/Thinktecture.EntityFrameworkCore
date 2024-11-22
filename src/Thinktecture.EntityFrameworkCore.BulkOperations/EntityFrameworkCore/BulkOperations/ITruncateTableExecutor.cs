namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Truncates table.
/// </summary>
public interface ITruncateTableExecutor
{
   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
      where T : class;

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/>.
   /// </summary>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task TruncateTableAsync(Type type, CancellationToken cancellationToken = default);
}
