using System.Data.Common;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options for the <see cref="SqliteTestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SqliteTestDbContextProviderOptions<T> : TestDbContextProviderOptions<T>
   where T : DbContext
{
   /// <summary>
   /// A factory method for creation of contexts of type <typeparamref name="T"/>.
   /// </summary>
   public Func<DbContextOptions<T>, T>? ContextFactory { get; set; }

   /// <summary>
   /// The connection string.
   /// </summary>
   public string ConnectionString { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTestDbContextProviderOptions{T}"/>
   /// </summary>
   public SqliteTestDbContextProviderOptions(
      DbConnection masterConnection,
      IMigrationExecutionStrategy migrationExecutionStrategy,
      DbContextOptions<T> masterDbContextOptions,
      DbContextOptions<T> dbContextOptions,
      TestingLoggingOptions testingLoggingOptions,
      IReadOnlyList<Action<T>> contextInitializations,
      string connectionString)
      : base(masterConnection, migrationExecutionStrategy, masterDbContextOptions, dbContextOptions, testingLoggingOptions, contextInitializations)
   {
      ConnectionString = connectionString;
   }
}
