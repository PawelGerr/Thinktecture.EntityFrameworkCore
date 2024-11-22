using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// A base class for integration tests using EF Core along with SQL Server.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
public abstract class SqlServerDbContextIntegrationTests<T> : ITestDbContextProvider<T>, IAsyncLifetime
   where T : DbContext
{
   private SqlServerTestDbContextProvider<T>? _testCtxProvider;

   /// <summary>
   /// Gets the <see cref="SqlServerTestDbContextProvider{T}"/> which is created on the first access.
   /// </summary>
   protected SqlServerTestDbContextProvider<T> TestCtxProvider => _testCtxProvider ??= TestCtxProviderBuilder.Build();

   private readonly SqlServerTestDbContextProviderBuilder<T> _testCtxProviderBuilder;
   private bool _isDisposed;
   private bool _isProviderConfigured;

   /// <summary>
   /// Gets the <see cref="SqlServerTestDbContextProviderBuilder{T}"/> which is created on the first access.
   /// </summary>
   protected SqlServerTestDbContextProviderBuilder<T> TestCtxProviderBuilder
   {
      get
      {
         if (!_isProviderConfigured)
         {
            ConfigureTestDbContextProvider(_testCtxProviderBuilder);
            _isProviderConfigured = true;
         }

         return _testCtxProviderBuilder;
      }
   }

   /// <inheritdoc />
   public T ArrangeDbContext => TestCtxProvider.ArrangeDbContext;

   /// <inheritdoc />
   public T ActDbContext => TestCtxProvider.ActDbContext;

   /// <inheritdoc />
   public T AssertDbContext => TestCtxProvider.AssertDbContext;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerDbContextIntegrationTests{T}"/>.
   /// </summary>
   /// <param name="connectionString">Connection string to use.</param>
   /// <param name="useSharedTables">Indication whether to create new tables with a new schema or use the existing ones.</param>
   /// <param name="testOutputHelper">Output helper to use for logging.</param>
   [Obsolete($"Use the overload with '{nameof(ITestIsolationOptions)}' instead.")]
   protected SqlServerDbContextIntegrationTests(
      string connectionString,
      bool useSharedTables = true,
      ITestOutputHelper? testOutputHelper = null)
      : this(connectionString,
             useSharedTables ? ITestIsolationOptions.SharedTablesAmbientTransaction : ITestIsolationOptions.RollbackMigrationsAndCleanup,
             testOutputHelper)
   {
   }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerDbContextIntegrationTests{T}"/>.
   /// </summary>
   /// <param name="connectionString">Connection string to use.</param>
   /// <param name="isolationOptions">Test isolation behavior.</param>
   /// <param name="testOutputHelper">Output helper to use for logging.</param>
   protected SqlServerDbContextIntegrationTests(
      string connectionString,
      ITestIsolationOptions isolationOptions,
      ITestOutputHelper? testOutputHelper = null)
   {
      _testCtxProviderBuilder = new SqlServerTestDbContextProviderBuilder<T>(connectionString, isolationOptions)
         .UseLogging(testOutputHelper);
   }

   /// <summary>
   /// Allows further configuration of the <see cref="SqlServerTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="builder">Builder for further configuration of <see cref="SqlServerTestDbContextProvider{T}"/>.</param>
   protected virtual void ConfigureTestDbContextProvider(SqlServerTestDbContextProviderBuilder<T> builder)
   {
   }

   /// <inheritdoc />
   public T CreateDbContext()
   {
      return TestCtxProvider.CreateDbContext();
   }

   /// <inheritdoc />
   public T CreateDbContext(bool useMasterConnection)
   {
      return TestCtxProvider.CreateDbContext(useMasterConnection);
   }

   /// <inheritdoc />
   public virtual Task InitializeAsync()
   {
      return Task.CompletedTask;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      Dispose(true);
      GC.SuppressFinalize(this);
   }

   /// <inheritdoc />
   async Task IAsyncLifetime.DisposeAsync()
   {
      await DisposeAsync();
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
   /// Disposes of managed resources like the <see cref="SqlServerTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="disposing">Indication that the method is being called by <see cref="Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      DisposeAsync(disposing).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
   }

   /// <summary>
   /// Disposes of managed resources like the <see cref="SqlServerTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="disposing">Indication that the method is being called by <see cref="Dispose()"/>.</param>
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if (!disposing)
         return;

      await (_testCtxProvider?.DisposeAsync() ?? ValueTask.CompletedTask);
      _testCtxProvider = null;
   }
}
