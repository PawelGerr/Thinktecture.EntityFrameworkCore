using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates temp tables.
/// </summary>
public interface ITempTableCreator
{
   /// <summary>
   /// Creates a temp table.
   /// </summary>
   /// <param name="entityType">Entity/query type.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A reference to a temp table.</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="entityType"/> is <c>null</c>.
   /// </exception>
   /// <exception cref="ArgumentException">The provided type <paramref name="entityType"/> is not known by the current <see cref="DbContext"/>.</exception>
   Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken = default);
}
