using System.Data;
using System.Diagnostics.CodeAnalysis;
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

   private readonly ITestIsolationOptions? _isolationOptions;

   /// <summary>
   /// Test isolation behavior.
   /// </summary>
   public ITestIsolationOptions IsolationOptions
   {
      get => _isolationOptions ?? ITestIsolationOptions.SharedTablesAmbientTransaction;
      init => _isolationOptions = value;
   }

   /// <summary>
   /// Default database schema to use.
   /// </summary>
   public string? Schema { get; }

   /// <summary>
   /// A factory method for creation of contexts of type <typeparamref name="T"/>.
   /// </summary>
   public Func<DbContextOptions<T>, IDbDefaultSchema?, T?>? ContextFactory { get; init; }

   /// <summary>
   /// Isolation level to be used with shared tables.
   /// Default is <see cref="IsolationLevel.ReadCommitted"/>.
   /// </summary>
   public IsolationLevel? SharedTablesIsolationLevel { get; init; }

   private SqlServerLockTableOptions? _lockTable;

   /// <summary>
   /// Options used for locking the database during migrations and tear down.
   /// </summary>
   [AllowNull]
   public SqlServerLockTableOptions LockTable
   {
      get => _lockTable ??= new SqlServerLockTableOptions(true);
      init => _lockTable = value;
   }

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
      string? schema)
      : base(masterConnection, migrationExecutionStrategy, masterDbContextOptionsBuilder, dbContextOptionsBuilder, testingLoggingOptions, contextInitializations)
   {
      Schema = schema;
   }
}
