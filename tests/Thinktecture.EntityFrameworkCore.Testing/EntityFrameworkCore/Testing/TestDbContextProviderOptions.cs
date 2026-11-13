using System.Data.Common;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options for the <see cref="ITestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TestDbContextProviderOptions<T>
   where T : DbContext
{
   /// <summary>
   /// Master database connection.
   /// </summary>
   public DbConnection MasterConnection { get; }

   /// <summary>
   /// Determines whether and how to migrate the database.
   /// </summary>
   public IMigrationExecutionStrategy MigrationExecutionStrategy { get; }

   /// <summary>
   /// Options that use the <see cref="MasterConnection"/>.
   /// </summary>
   public DbContextOptionsBuilder<T> MasterDbContextOptionsBuilder { get; }

   /// <summary>
   /// Options that create a new connection.
   /// </summary>
   public DbContextOptionsBuilder<T> DbContextOptionsBuilder { get; }

   /// <summary>
   /// Contains executed commands if this feature was activated.
   /// </summary>
   public TestingLoggingOptions TestingLoggingOptions { get; }

   /// <summary>
   /// Callback to execute on every creation of a new <see cref="DbContext"/>.
   /// </summary>
   public IReadOnlyList<Action<T>> ContextInitializations { get; }

   /// <summary>
   /// Contains executed commands if this feature was activated.
   /// </summary>
   public IReadOnlyCollection<string>? ExecutedCommands { get; init; }

   /// <summary>
   /// Initializes new instance of <see cref="TestDbContextProviderOptions{T}"/>.
   /// </summary>
   protected TestDbContextProviderOptions(
      DbConnection masterConnection,
      IMigrationExecutionStrategy migrationExecutionStrategy,
      DbContextOptionsBuilder<T> masterDbContextOptionsBuilder,
      DbContextOptionsBuilder<T> dbContextOptionsBuilder,
      TestingLoggingOptions testingLoggingOptions,
      IReadOnlyList<Action<T>> contextInitializations)
   {
      MasterConnection = masterConnection;
      MigrationExecutionStrategy = migrationExecutionStrategy;
      MasterDbContextOptionsBuilder = masterDbContextOptionsBuilder;
      DbContextOptionsBuilder = dbContextOptionsBuilder;
      ContextInitializations = contextInitializations;
      TestingLoggingOptions = testingLoggingOptions;
   }
}
