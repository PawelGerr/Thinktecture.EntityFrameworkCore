using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
public abstract class SqlServerTestDbContextProvider
{
   private static readonly object _sharedLock = new();

   /// <summary>
   /// Provides a lock object for database-wide operations like creation of tables.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <returns>A lock object.</returns>
   protected virtual object GetSharedLock(DbContext ctx)
   {
      return _sharedLock;
   }
}

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public class SqlServerTestDbContextProvider<T> : SqlServerTestDbContextProvider, ITestDbContextProvider<T>
   where T : DbContext
{
   private readonly object _instanceWideLock;
   private readonly ITestIsolationOptions _isolationOptions;
   private readonly DbContextOptions<T> _masterDbContextOptions;
   private readonly DbContextOptions<T> _dbContextOptions;
   private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;
   private readonly DbConnection _masterConnection;
   private readonly IReadOnlyList<Action<T>> _contextInitializations;
   private readonly Func<DbContextOptions<T>, IDbDefaultSchema, T?>? _contextFactory;
   private readonly TestingLoggingOptions _testingLoggingOptions;

   private T? _arrangeDbContext;
   private T? _actDbContext;
   private T? _assertDbContext;
   private IDbContextTransaction? _tx;
   private bool _isAtLeastOneContextCreated;
   private readonly IsolationLevel _sharedTablesIsolationLevel;
   private readonly IsolationLevel _migrationAndCleanupIsolationLevel;

   /// <inheritdoc />
   public T ArrangeDbContext => _arrangeDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T ActDbContext => _actDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T AssertDbContext => _assertDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database schema to use.
   /// </summary>
   // ReSharper disable once MemberCanBePrivate.Global
   public string Schema { get; }

   /// <summary>
   /// Contains executed commands if this feature was activated.
   /// </summary>
   public IReadOnlyCollection<string>? ExecutedCommands { get; }

   /// <summary>
   /// Log level switch.
   /// </summary>
   public TestingLogLevelSwitch LogLevelSwitch => _testingLoggingOptions.LogLevelSwitch;

   /// <summary>
   /// Initializes a new instance of <see cref="SqlServerTestDbContextProvider{T}"/>
   /// </summary>
   /// <param name="options">Options.</param>
   protected internal SqlServerTestDbContextProvider(SqlServerTestDbContextProviderOptions<T> options)
   {
      ArgumentNullException.ThrowIfNull(options);

      _instanceWideLock = new object();
      Schema = options.Schema ?? throw new ArgumentException($"The '{nameof(options.Schema)}' cannot be null.", nameof(options));
      _sharedTablesIsolationLevel = ValidateIsolationLevel(options.SharedTablesIsolationLevel);
      _migrationAndCleanupIsolationLevel = options.MigrationAndCleanupIsolationLevel ?? IsolationLevel.Serializable;
      _isolationOptions = options.IsolationOptions;
      _masterConnection = options.MasterConnection ?? throw new ArgumentException($"The '{nameof(options.MasterConnection)}' cannot be null.", nameof(options));
      _masterDbContextOptions = options.MasterDbContextOptions ?? throw new ArgumentException($"The '{nameof(options.MasterDbContextOptions)}' cannot be null.", nameof(options));
      _dbContextOptions = options.DbContextOptions ?? throw new ArgumentException($"The '{nameof(options.DbContextOptions)}' cannot be null.", nameof(options));
      _migrationExecutionStrategy = options.MigrationExecutionStrategy ?? throw new ArgumentException($"The '{nameof(options.MigrationExecutionStrategy)}' cannot be null.", nameof(options));
      _testingLoggingOptions = options.TestingLoggingOptions ?? throw new ArgumentException($"The '{nameof(options.TestingLoggingOptions)}' cannot be null.", nameof(options));
      _contextInitializations = options.ContextInitializations ?? throw new ArgumentException($"The '{nameof(options.ContextInitializations)}' cannot be null.", nameof(options));
      ExecutedCommands = options.ExecutedCommands;
      _contextFactory = options.ContextFactory;
   }

   private static IsolationLevel ValidateIsolationLevel(IsolationLevel? isolationLevel)
   {
      if (!isolationLevel.HasValue)
         return IsolationLevel.ReadCommitted;

      if (!Enum.IsDefined(isolationLevel.Value))
         throw new ArgumentException($"The provided isolation level '{isolationLevel}' is invalid.", nameof(isolationLevel));

      if (isolationLevel < IsolationLevel.ReadCommitted)
         throw new ArgumentException($"The isolation level '{isolationLevel}' cannot be less than '{nameof(IsolationLevel.ReadCommitted)}'.", nameof(isolationLevel));

      return isolationLevel.Value;
   }

   /// <inheritdoc />
   public T CreateDbContext()
   {
      return CreateDbContext(false);
   }

   /// <summary>
   /// Creates a new <see cref="DbContext"/>.
   /// </summary>
   /// <param name="useMasterConnection">
   /// Indication whether to use the master connection or a new one.
   /// </param>
   /// <returns>A new instance of <typeparamref name="T"/>.</returns>
   public T CreateDbContext(bool useMasterConnection)
   {
      if (!useMasterConnection && _isolationOptions == ITestIsolationOptions.SharedTablesAmbientTransaction)
         throw new NotSupportedException($"A database transaction cannot be shared among different connections, so the isolation of tests couldn't be guaranteed. Set 'useMasterConnection' to 'true' or 'useSharedTables' to 'false' or use '{nameof(ArrangeDbContext)}/{nameof(ActDbContext)}/{nameof(AssertDbContext)}' which use the same database connection.");

      bool isFirstCtx;

      lock (_instanceWideLock)
      {
         isFirstCtx = !_isAtLeastOneContextCreated;
         _isAtLeastOneContextCreated = true;
      }

      var options = useMasterConnection ? _masterDbContextOptions : _dbContextOptions;
      var ctx = CreateDbContext(options, new DbDefaultSchema(Schema));

      foreach (var ctxInit in _contextInitializations)
      {
         ctxInit(ctx);
      }

      if (isFirstCtx)
      {
         RunMigrations(ctx);

         if (_isolationOptions == ITestIsolationOptions.SharedTablesAmbientTransaction)
            _tx = BeginTransaction(ctx);
      }
      else if (_tx != null)
      {
         ctx.Database.UseTransaction(_tx.GetDbTransaction());
      }

      return ctx;
   }

   /// <summary>
   /// Creates a new instance of the database context.
   /// </summary>
   /// <param name="options">Options to use for creation.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>A new instance of the database context.</returns>
   protected virtual T CreateDbContext(DbContextOptions<T> options, IDbDefaultSchema schema)
   {
      var ctx = _contextFactory?.Invoke(options, schema);

      if (ctx is not null)
         return ctx;

      ctx = (T?)Activator.CreateInstance(typeof(T), options, schema);

      if (ctx is null)
      {
         if (_isolationOptions != ITestIsolationOptions.SharedTablesAmbientTransaction)
         {
            throw new Exception(@$"Could not create an instance of type of '{typeof(T).ShortDisplayName()}' using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options, {nameof(IDbDefaultSchema)} schema).
Please provide the corresponding constructor or a custom factory via '{typeof(SqlServerTestDbContextProviderBuilder<T>).ShortDisplayName()}.{nameof(SqlServerTestDbContextProviderBuilder<T>.UseContextFactory)}'.");
         }

         ctx = (T)(Activator.CreateInstance(typeof(T), options)
                   ?? throw new Exception(@$"Could not create an instance of type of '{typeof(T).ShortDisplayName()}' neither using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options, {nameof(IDbDefaultSchema)} schema) nor using construct ({typeof(DbContextOptions<T>).ShortDisplayName()} options).
Please provide the corresponding constructor or a custom factory via '{typeof(SqlServerTestDbContextProviderBuilder<T>).ShortDisplayName()}.{nameof(SqlServerTestDbContextProviderBuilder<T>.UseContextFactory)}'."));
      }

      return ctx;
   }

   /// <summary>
   /// Starts a new transaction.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <returns>An instance of <see cref="IDbContextTransaction"/>.</returns>
   protected virtual IDbContextTransaction BeginTransaction(T ctx)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      return ctx.Database.BeginTransaction(_sharedTablesIsolationLevel);
   }

   /// <summary>
   /// Runs migrations for provided <paramref name="ctx" />.
   /// </summary>
   /// <param name="ctx">Database context to run migrations for.</param>
   /// <exception cref="ArgumentNullException">The provided context is <c>null</c>.</exception>
   protected virtual void RunMigrations(T ctx)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      // concurrent execution is not supported by EF migrations
      lock (GetSharedLock(ctx))
      {
         var logLevel = LogLevelSwitch.MinimumLogLevel;

         try
         {
            LogLevelSwitch.MinimumLogLevel = _testingLoggingOptions.MigrationLogLevel;

            IDbContextTransaction? migrationTx = null;

            if (ctx.Database.CurrentTransaction is null)
               migrationTx = ctx.Database.BeginTransaction(_migrationAndCleanupIsolationLevel);

            try
            {
               _migrationExecutionStrategy.Migrate(ctx);
            }
            finally
            {
               migrationTx?.Commit();
            }
         }
         finally
         {
            LogLevelSwitch.MinimumLogLevel = logLevel;
         }
      }
   }

   /// <summary>
   /// Rollbacks transaction if shared tables are used
   /// otherwise the migrations are rolled back and all tables, functions, views and the newly generated schema are deleted.
   /// </summary>
   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqlServerTestDbContextProvider{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      if (!disposing)
         return;

      var isAtLeastOneContextCreated = false;

      lock (_instanceWideLock)
      {
         if (_isAtLeastOneContextCreated)
         {
            isAtLeastOneContextCreated = true;
            _isAtLeastOneContextCreated = false;
         }
      }

      if (isAtLeastOneContextCreated)
         DisposeContextsAndRollbackMigrations();

      _masterConnection.Dispose();
      _testingLoggingOptions.Dispose();
   }

   private void DisposeContextsAndRollbackMigrations()
   {
      _tx?.Rollback();
      _tx?.Dispose();

      if (_isolationOptions.NeedsCleanup)
      {
         // Create a new ctx as a last resort to rollback migrations and clean up the database
         using var ctx = _actDbContext ?? _arrangeDbContext ?? _assertDbContext ?? CreateDbContext(_masterDbContextOptions, new DbDefaultSchema(Schema));

         lock (GetSharedLock(ctx))
         {
            IDbContextTransaction? migrationTx = null;

            if (ctx.Database.CurrentTransaction is null)
               migrationTx = ctx.Database.BeginTransaction(_migrationAndCleanupIsolationLevel);

            try
            {
               _isolationOptions.Cleanup(ctx, Schema);
            }
            finally
            {
               migrationTx?.Commit();
            }
         }
      }

      _arrangeDbContext?.Dispose();
      _actDbContext?.Dispose();
      _assertDbContext?.Dispose();

      _arrangeDbContext = null;
      _actDbContext = null;
      _assertDbContext = null;
   }
}
