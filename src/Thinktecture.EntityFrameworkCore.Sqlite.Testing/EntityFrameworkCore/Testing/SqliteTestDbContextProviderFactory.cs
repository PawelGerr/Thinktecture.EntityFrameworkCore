using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Thinktecture.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// A factory for creation of <see cref="SqliteTestDbContextProvider{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
public class SqliteTestDbContextProviderFactory<T> : IAsyncLifetime, IAsyncDisposable, IDisposable
   where T : DbContext
{
   private readonly SqliteTestDbContextProviderBuilder<T> _builder;
   private readonly SqliteConnection _masterConnection;

   private bool _isInitialized;

   /// <summary>
   /// Creates new factory.
   /// </summary>
   /// <param name="builder">Context builder for creation of new instances of <see cref="SqliteTestDbContextProvider{T}"/>.</param>
   public SqliteTestDbContextProviderFactory(SqliteTestDbContextProviderBuilder<T> builder)
   {
      _builder = builder;
      _masterConnection = new SqliteConnection(SqliteTestDbContextProviderBuilder<T>.CreateRandomConnectionString());
   }

   /// <summary>
   /// Creates a new <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="testOutputHelper">Output helper to use.</param>
   /// <param name="migrationStrategy">Execution strategy to use.</param>
   /// <returns>A new instance of <see cref="SqliteTestDbContextProvider{T}"/>.</returns>
   /// <exception cref="InvalidOperationException">If the factory is not initialized.</exception>
   public SqliteTestDbContextProvider<T> Create(
      ITestOutputHelper testOutputHelper,
      IMigrationExecutionStrategy? migrationStrategy = null)
   {
      return Create(_builder.CreateLoggingOptions(testOutputHelper.ToLoggerFactory(), null), migrationStrategy);
   }

   /// <summary>
   /// Creates a new <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="loggerFactory">Logging factory to use.</param>
   /// <param name="migrationStrategy">Execution strategy to use.</param>
   /// <returns>A new instance of <see cref="SqliteTestDbContextProvider{T}"/>.</returns>
   /// <exception cref="InvalidOperationException">If the factory is not initialized.</exception>
   public SqliteTestDbContextProvider<T> Create(
      ILoggerFactory loggerFactory,
      IMigrationExecutionStrategy? migrationStrategy = null)
   {
      return Create(_builder.CreateLoggingOptions(loggerFactory, null), migrationStrategy);
   }

   /// <summary>
   /// Creates a new <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="logger">Logger to use.</param>
   /// <param name="migrationStrategy">Execution strategy to use.</param>
   /// <returns>A new instance of <see cref="SqliteTestDbContextProvider{T}"/>.</returns>
   /// <exception cref="InvalidOperationException">If the factory is not initialized.</exception>
   public SqliteTestDbContextProvider<T> Create(
      Serilog.ILogger logger,
      IMigrationExecutionStrategy? migrationStrategy = null)
   {
      return Create(_builder.CreateLoggingOptions(null, logger), migrationStrategy);
   }

   /// <summary>
   /// Creates a new <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="loggingOptions">Logging options to use.</param>
   /// <param name="migrationStrategy">Execution strategy to use.</param>
   /// <returns>A new instance of <see cref="SqliteTestDbContextProvider{T}"/>.</returns>
   /// <exception cref="InvalidOperationException">If the factory is not initialized.</exception>
   public SqliteTestDbContextProvider<T> Create(
      TestingLoggingOptions? loggingOptions = null,
      IMigrationExecutionStrategy? migrationStrategy = null)
   {
      if (!_isInitialized)
         throw new InvalidOperationException("The factory is not initialized yet.");

      loggingOptions ??= TestingLoggingOptions.Empty;

      var provider = _builder.Build(loggingOptions, migrationStrategy ?? IMigrationExecutionStrategy.NoMigration);
      provider.MasterConnection.Open();

      _masterConnection.BackupDatabase(provider.MasterConnection, "main", "main");

      return provider;
   }

   /// <summary>
   /// Initializes the factory.
   /// </summary>
   public void Initialize()
   {
      InitializeAsync().GetAwaiter().GetResult();
   }

   /// <summary>
   /// Initializes the factory.
   /// </summary>
   public async Task InitializeAsync()
   {
      await _masterConnection.OpenAsync();

      await using var initializationProvider = _builder.Build(_masterConnection);
      await using var ctx = initializationProvider.CreateDbContext(true); // runs migrations

      _isInitialized = true;
   }

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      await _masterConnection.DisposeAsync();
   }

   async Task IAsyncLifetime.DisposeAsync()
   {
      await _masterConnection.DisposeAsync();
   }

   /// <inheritdoc />
   public void Dispose()
   {
      _masterConnection.Dispose();
   }
}
