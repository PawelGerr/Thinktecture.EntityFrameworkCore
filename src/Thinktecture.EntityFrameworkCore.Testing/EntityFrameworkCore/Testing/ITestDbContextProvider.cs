namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public interface ITestDbContextProvider<T> : IDbContextFactory<T>, IAsyncDisposable, IDisposable
   where T : DbContext
{
   /// <summary>
   /// Database context for setting up the test data.
   /// </summary>
   T ArrangeDbContext { get; }

   /// <summary>
   /// Database context for the actual test.
   /// </summary>
   T ActDbContext { get; }

   /// <summary>
   /// Database context for making assertions.
   /// </summary>
   T AssertDbContext { get; }

   /// <summary>
   /// Creates a new <see cref="DbContext"/>.
   /// </summary>
   /// <param name="useMasterConnection">
   /// Indication whether to use the master connection or a new one.
   /// </param>
   /// <returns>A new instance of <typeparamref name="T"/>.</returns>
   T CreateDbContext(bool useMasterConnection);
}
