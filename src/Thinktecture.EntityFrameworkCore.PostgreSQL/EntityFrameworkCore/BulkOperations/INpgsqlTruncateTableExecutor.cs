namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// PostgreSQL-specific truncate table executor that supports CASCADE.
/// </summary>
public interface INpgsqlTruncateTableExecutor : ITruncateTableExecutor
{
   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="cascade">If <c>true</c>, CASCADE is appended to the TRUNCATE statement.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   Task TruncateTableAsync<T>(bool cascade, CancellationToken cancellationToken = default)
      where T : class;

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/>.
   /// </summary>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="cascade">If <c>true</c>, CASCADE is appended to the TRUNCATE statement.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task TruncateTableAsync(Type type, bool cascade, CancellationToken cancellationToken = default);
}
