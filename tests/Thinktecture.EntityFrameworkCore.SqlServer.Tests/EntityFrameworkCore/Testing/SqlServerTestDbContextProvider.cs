using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.Logging;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public class SqlServerTestDbContextProvider<T> : IAsyncDisposable, IDisposable
   where T : DbContext
{
   private static readonly Lock _migrationLock = new();

   private readonly Lock _instanceWideLock;
   private readonly ITestIsolationOptions _isolationOptions;
   private readonly DbContextOptions<T> _masterDbContextOptions;
   private readonly DbContextOptions<T> _dbContextOptions;
   private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;
   private readonly DbConnection _masterConnection;
   private readonly IReadOnlyList<Action<T>> _contextInitializations;
   private readonly TestingLoggingOptions _testingLoggingOptions;

   private Func<DbContextOptions<T>, IDbDefaultSchema?, T>? _defaultContextFactory;
   private T? _arrangeDbContext;
   private T? _actDbContext;
   private T? _assertDbContext;
   private bool _isAtLeastOneContextCreated;
   private bool _isDisposed;

   /// <summary>
   /// Database context for setting up the test data.
   /// </summary>
   public T ArrangeDbContext => _arrangeDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database context for the actual test.
   /// </summary>
   public T ActDbContext => _actDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Database context for making assertions.
   /// </summary>
   public T AssertDbContext => _assertDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Default database schema to use.
   /// </summary>
   // ReSharper disable once MemberCanBePrivate.Global
   public string? Schema { get; }

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

      _instanceWideLock = new Lock();
      Schema = options.Schema;
      _isolationOptions = options.IsolationOptions;
      _masterConnection = options.MasterConnection ?? throw new ArgumentException($"The '{nameof(options.MasterConnection)}' cannot be null.", nameof(options));
      _masterDbContextOptions = options.MasterDbContextOptionsBuilder.Options ?? throw new ArgumentException($"The '{nameof(options.MasterDbContextOptionsBuilder)}' cannot be null.", nameof(options));
      _dbContextOptions = options.DbContextOptionsBuilder.Options ?? throw new ArgumentException($"The '{nameof(options.DbContextOptionsBuilder)}' cannot be null.", nameof(options));
      _migrationExecutionStrategy = options.MigrationExecutionStrategy ?? throw new ArgumentException($"The '{nameof(options.MigrationExecutionStrategy)}' cannot be null.", nameof(options));
      _testingLoggingOptions = options.TestingLoggingOptions ?? throw new ArgumentException($"The '{nameof(options.TestingLoggingOptions)}' cannot be null.", nameof(options));
      _contextInitializations = options.ContextInitializations ?? throw new ArgumentException($"The '{nameof(options.ContextInitializations)}' cannot be null.", nameof(options));
      ExecutedCommands = options.ExecutedCommands;
   }

   /// <summary>
   /// Creates a new <see cref="DbContext"/> with a new connection.
   /// </summary>
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
      bool isFirstCtx;

      lock (_instanceWideLock)
      {
         isFirstCtx = !_isAtLeastOneContextCreated;
         _isAtLeastOneContextCreated = true;
      }

      var options = useMasterConnection ? _masterDbContextOptions : _dbContextOptions;
      var ctx = CreateDbContext(options, Schema is null ? null : new DbDefaultSchema(Schema));

      foreach (var ctxInit in _contextInitializations)
      {
         ctxInit(ctx);
      }

      if (isFirstCtx)
         RunMigrations(ctx);

      return ctx;
   }

   /// <summary>
   /// Creates a new instance of the database context.
   /// </summary>
   /// <param name="options">Options to use for creation.</param>
   /// <param name="schema">Database schema to use.</param>
   /// <returns>A new instance of the database context.</returns>
   protected virtual T CreateDbContext(DbContextOptions<T> options, IDbDefaultSchema? schema)
   {
      return (_defaultContextFactory ??= CreateDefaultContextFactory())(options, schema);
   }

   private static Func<DbContextOptions<T>, IDbDefaultSchema?, T> CreateDefaultContextFactory()
   {
      var optionsType = typeof(DbContextOptions<T>);
      var schemaType = typeof(IDbDefaultSchema);
      var optionsParam = Expression.Parameter(optionsType);
      var schemaParam = Expression.Parameter(schemaType);
      Expression[]? ctorArgs = null;

      var ctor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { optionsType, schemaType });

      if (ctor is not null)
      {
         ctorArgs = new Expression[] { optionsParam, schemaParam };
      }
      else
      {
         ctor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { optionsType });

         if (ctor is not null)
            ctorArgs = new Expression[] { optionsParam };
      }

      if (ctor is null || ctorArgs is null)
      {
         throw new Exception($"""
                              Could not create an instance of type of '{typeof(T).ShortDisplayName()}' neither using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options, {nameof(IDbDefaultSchema)} schema) nor using construct ({typeof(DbContextOptions<T>).ShortDisplayName()} options).
                              """);
      }

      return Expression.Lambda<Func<DbContextOptions<T>, IDbDefaultSchema?, T>>(Expression.New(ctor, ctorArgs), optionsParam, schemaParam)
                       .Compile();
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
      lock (_migrationLock)
      {
         var logLevel = LogLevelSwitch.MinimumLogLevel;

         try
         {
            LogLevelSwitch.MinimumLogLevel = _testingLoggingOptions.MigrationLogLevel;
            _migrationExecutionStrategy.Migrate(ctx);
         }
         finally
         {
            LogLevelSwitch.MinimumLogLevel = logLevel;
         }
      }
   }

   /// <summary>
   /// Performs the cleanup of the <see cref="ITestIsolationOptions"/> and disposes of the contexts and the master connection.
   /// </summary>
   public void Dispose()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      Dispose(true);
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Performs the cleanup of the <see cref="ITestIsolationOptions"/> and disposes of the contexts and the master connection.
   /// </summary>
   public async ValueTask DisposeAsync()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      await DisposeAsync(true);
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqlServerTestDbContextProvider{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      DisposeAsync(disposing).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqlServerTestDbContextProvider{T}.Dispose()"/>.</param>
   protected virtual async ValueTask DisposeAsync(bool disposing)
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
         await CleanupAndDisposeContextsAsync(default);

      _masterConnection.Dispose();
      _testingLoggingOptions.Dispose();
   }

   private async Task CleanupAndDisposeContextsAsync(CancellationToken cancellationToken)
   {
      // Create a new ctx as a last resort to clean up the database
      await using (var ctx = _actDbContext ?? _arrangeDbContext ?? _assertDbContext ?? CreateDbContext(_masterDbContextOptions, Schema is null ? null : new DbDefaultSchema(Schema)))
      {
         await _isolationOptions.CleanupAsync(ctx, cancellationToken);
      }

      await (_arrangeDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
      await (_actDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
      await (_assertDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);

      _arrangeDbContext = null;
      _actDbContext = null;
      _assertDbContext = null;
   }
}
