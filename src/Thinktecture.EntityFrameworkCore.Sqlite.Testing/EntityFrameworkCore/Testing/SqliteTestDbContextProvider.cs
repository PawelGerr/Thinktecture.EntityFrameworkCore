using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.Logging;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Provides instances of <see cref="DbContext"/> for testing purposes.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public class SqliteTestDbContextProvider<T> : ITestDbContextProvider<T>
   where T : DbContext
{
   // ReSharper disable once StaticMemberInGenericType because the lock are all for the same database context.
   private static readonly object _lock = new();

   private readonly DbContextOptions<T> _masterDbContextOptions;
   private readonly DbContextOptions<T> _dbContextOptions;
   private readonly IMigrationExecutionStrategy _migrationExecutionStrategy;
   private readonly DbConnection _masterConnection;
   private readonly IReadOnlyList<Action<T>> _contextInitializations;
   private readonly Func<DbContextOptions<T>, T?>? _contextFactory;

   private T? _arrangeDbContext;
   private T? _actDbContext;
   private T? _assertDbContext;
   private bool _isAtLeastOneContextCreated;
   private readonly TestingLoggingOptions _testingLoggingOptions;
   private bool _isDisposed;

   /// <inheritdoc />
   public T ArrangeDbContext => _arrangeDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T ActDbContext => _actDbContext ??= CreateDbContext(true);

   /// <inheritdoc />
   public T AssertDbContext => _assertDbContext ??= CreateDbContext(true);

   /// <summary>
   /// Contains executed commands if this feature was activated.
   /// </summary>
   public IReadOnlyCollection<string>? ExecutedCommands { get; }

   /// <summary>
   /// Log level switch.
   /// </summary>
   public TestingLogLevelSwitch LogLevelSwitch => _testingLoggingOptions.LogLevelSwitch;

   /// <summary>
   /// The connection string.
   /// </summary>
   public string ConnectionString { get; }

   /// <summary>
   /// Initializes a new instance of <see cref="SqliteTestDbContextProvider{T}"/>
   /// </summary>
   /// <param name="options">Options.</param>
   protected internal SqliteTestDbContextProvider(SqliteTestDbContextProviderOptions<T> options)
   {
      ArgumentNullException.ThrowIfNull(options);

      ConnectionString = options.ConnectionString ?? throw new ArgumentException($"The '{nameof(options.ConnectionString)}' cannot be null.", nameof(options));
      _masterConnection = options.MasterConnection ?? throw new ArgumentException($"The '{nameof(options.MasterConnection)}' cannot be null.", nameof(options));
      _masterDbContextOptions = options.MasterDbContextOptions ?? throw new ArgumentException($"The '{nameof(options.MasterDbContextOptions)}' cannot be null.", nameof(options));
      _dbContextOptions = options.DbContextOptions ?? throw new ArgumentException($"The '{nameof(options.DbContextOptions)}' cannot be null.", nameof(options));
      _migrationExecutionStrategy = options.MigrationExecutionStrategy ?? throw new ArgumentException($"The '{nameof(options.MigrationExecutionStrategy)}' cannot be null.", nameof(options));
      _testingLoggingOptions = options.TestingLoggingOptions ?? throw new ArgumentException($"The '{nameof(options.TestingLoggingOptions)}' cannot be null.", nameof(options));
      _contextInitializations = options.ContextInitializations ?? throw new ArgumentException($"The '{nameof(options.ContextInitializations)}' cannot be null.", nameof(options));
      ExecutedCommands = options.ExecutedCommands;
      _contextFactory = options.ContextFactory;
   }

   /// <inheritdoc />
   public T CreateDbContext()
   {
      return CreateDbContext(false);
   }

   /// <inheritdoc />
   public T CreateDbContext(bool useMasterConnection)
   {
      bool isFirstCtx;

      lock (_lock)
      {
         isFirstCtx = !_isAtLeastOneContextCreated;
         _isAtLeastOneContextCreated = true;
      }

      var options = useMasterConnection ? _masterDbContextOptions : _dbContextOptions;
      var ctx = CreateDbContext(options);

      foreach (var ctxInit in _contextInitializations)
      {
         ctxInit(ctx);
      }

      if (isFirstCtx)
      {
         _masterConnection.Open();
         RunMigrations(ctx);
      }

      return ctx;
   }

   /// <summary>
   /// Creates a new instance of the database context.
   /// </summary>
   /// <param name="options">Options to use for creation.</param>
   /// <returns>A new instance of the database context.</returns>
   protected virtual T CreateDbContext(DbContextOptions<T> options)
   {
      var ctx = _contextFactory?.Invoke(options);

      if (ctx is not null)
         return ctx;

      ctx = (T)(Activator.CreateInstance(typeof(T), options)
                ?? throw new Exception(@$"Could not create an instance of type of '{typeof(T).ShortDisplayName()}' using constructor parameters ({typeof(DbContextOptions<T>).ShortDisplayName()} options).
Please provide the corresponding constructor or a custom factory via '{typeof(SqliteTestDbContextProviderBuilder<T>).ShortDisplayName()}.{nameof(SqliteTestDbContextProviderBuilder<T>.UseContextFactory)}'."));

      return ctx;
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
      lock (_lock)
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
   /// Rollbacks transaction if shared tables are used
   /// otherwise the migrations are rolled back and all tables, functions, views and the newly generated schema are deleted.
   /// </summary>
   public void Dispose()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      Dispose(true);
      GC.SuppressFinalize(this);
   }

   /// <inheritdoc />
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
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqliteDbContextIntegrationTests{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      DisposeAsync(disposing).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqliteDbContextIntegrationTests{T}.Dispose()"/>.</param>
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if (!disposing)
         return;

      if (_isAtLeastOneContextCreated)
      {
         await DisposeContextsAsync();
         _isAtLeastOneContextCreated = false;
      }

      await _masterConnection.DisposeAsync();
      _testingLoggingOptions.Dispose();
   }

   private async ValueTask DisposeContextsAsync()
   {
      await (_arrangeDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
      await (_actDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);
      await (_assertDbContext?.DisposeAsync() ?? ValueTask.CompletedTask);

      _arrangeDbContext = null;
      _actDbContext = null;
      _assertDbContext = null;
   }
}
