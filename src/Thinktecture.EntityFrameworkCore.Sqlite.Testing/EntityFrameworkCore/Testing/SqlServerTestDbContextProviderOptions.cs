using Microsoft.Data.Sqlite;
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
   /// Master database connection.
   /// </summary>
   public new SqliteConnection MasterConnection => (SqliteConnection)base.MasterConnection;

   /// <summary>
   /// Indication whether the master connection is externally owned.
   /// </summary>
   public bool IsExternallyOwnedMasterConnection { get; }

   /// <summary>
   /// A factory method for creation of contexts of type <typeparamref name="T"/>.
   /// </summary>
   public Func<DbContextOptions<T>, T?>? ContextFactory { get; init; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTestDbContextProviderOptions{T}"/>
   /// </summary>
   public SqliteTestDbContextProviderOptions(
      SqliteConnection masterConnection,
      bool isExternallyOwnedMasterConnection,
      IMigrationExecutionStrategy migrationExecutionStrategy,
      DbContextOptionsBuilder<T> masterDbContextOptionsBuilder,
      DbContextOptionsBuilder<T> dbContextOptionsBuilder,
      TestingLoggingOptions testingLoggingOptions,
      IReadOnlyList<Action<T>> contextInitializations)
      : base(masterConnection, migrationExecutionStrategy, masterDbContextOptionsBuilder, dbContextOptionsBuilder, testingLoggingOptions, contextInitializations)
   {
      IsExternallyOwnedMasterConnection = isExternallyOwnedMasterConnection;
   }
}
