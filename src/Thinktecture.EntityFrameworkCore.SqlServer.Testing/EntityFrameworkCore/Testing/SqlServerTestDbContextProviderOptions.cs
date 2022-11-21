using System.Data;
using System.Data.Common;
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
   /// Indication whether the current <see cref="SqlServerTestDbContextProvider{T}"/> is using its own tables with a new schema
   /// or shares the tables with others.
   /// </summary>
   [Obsolete($"Use '{nameof(IsolationOptions)}' instead.")]
   public bool IsUsingSharedTables
   {
      get => IsolationOptions == ITestIsolationOptions.SharedTablesAmbientTransaction;
      set => IsolationOptions = value ? ITestIsolationOptions.SharedTablesAmbientTransaction : ITestIsolationOptions.RollbackMigrationsAndCleanup;
   }

   private ITestIsolationOptions? _isolationOptions;

   /// <summary>
   /// Test isolation behavior.
   /// </summary>
   public ITestIsolationOptions IsolationOptions
   {
      get => _isolationOptions ?? ITestIsolationOptions.SharedTablesAmbientTransaction;
      set => _isolationOptions = value;
   }

   /// <summary>
   /// Database schema to use.
   /// </summary>
   public string Schema { get; set; }

   /// <summary>
   /// A factory method for creation of contexts of type <typeparamref name="T"/>.
   /// </summary>
   public Func<DbContextOptions<T>, IDbDefaultSchema, T?>? ContextFactory { get; set; }

   /// <summary>
   /// Isolation level to be used with shared tables.
   /// </summary>
   public IsolationLevel? SharedTablesIsolationLevel { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTestDbContextProviderOptions{T}"/>.
   /// </summary>
   public SqlServerTestDbContextProviderOptions(
      DbConnection masterConnection,
      IMigrationExecutionStrategy migrationExecutionStrategy,
      DbContextOptions<T> masterDbContextOptions,
      DbContextOptions<T> dbContextOptions,
      TestingLoggingOptions testingLoggingOptions,
      IReadOnlyList<Action<T>> contextInitializations,
      string schema)
      : base(masterConnection, migrationExecutionStrategy, masterDbContextOptions, dbContextOptions, testingLoggingOptions, contextInitializations)
   {
      Schema = schema;
   }
}
