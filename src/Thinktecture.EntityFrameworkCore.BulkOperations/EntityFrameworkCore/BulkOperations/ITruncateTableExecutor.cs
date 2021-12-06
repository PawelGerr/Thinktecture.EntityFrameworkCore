namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Truncates table.
/// </summary>
public interface ITruncateTableExecutor
{
   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
      where T : class;
}
