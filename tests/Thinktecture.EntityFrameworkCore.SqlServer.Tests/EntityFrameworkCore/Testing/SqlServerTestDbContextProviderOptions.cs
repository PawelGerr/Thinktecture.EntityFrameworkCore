using Microsoft.Data.SqlClient;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options for the <see cref="SqlServerTestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SqlServerTestDbContextProviderOptions<T> : TestDbContextProviderOptions<T>
   where T : DbContext
{
   /// <summary>
   /// Master database connection.
   /// </summary>
   public new SqlConnection MasterConnection => (SqlConnection)base.MasterConnection;

   /// <summary>
   /// Test isolation behavior.
   /// </summary>
   public ITestIsolationOptions IsolationOptions { get; }

   /// <summary>
   /// Default database schema to use.
   /// </summary>
   public string? Schema { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTestDbContextProviderOptions{T}"/>.
   /// </summary>
   public SqlServerTestDbContextProviderOptions(
      SqlConnection masterConnection,
      IMigrationExecutionStrategy migrationExecutionStrategy,
      DbContextOptionsBuilder<T> masterDbContextOptionsBuilder,
      DbContextOptionsBuilder<T> dbContextOptionsBuilder,
      TestingLoggingOptions testingLoggingOptions,
      IReadOnlyList<Action<T>> contextInitializations,
      string? schema,
      ITestIsolationOptions isolationOptions)
      : base(masterConnection, migrationExecutionStrategy, masterDbContextOptionsBuilder, dbContextOptionsBuilder, testingLoggingOptions, contextInitializations)
   {
      Schema = schema;
      IsolationOptions = isolationOptions;
   }
}
