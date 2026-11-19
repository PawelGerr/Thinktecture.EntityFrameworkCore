using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// A base class for integration tests using EF Core along with SQLite.
/// </summary>
/// <typeparam name="T">Type of the database context.</typeparam>
// ReSharper disable once UnusedMember.Global
public abstract class SqliteDbContextIntegrationTests<T> : IAsyncLifetime, IAsyncDisposable, IDisposable
   where T : DbContext
{
   private SqliteTestDbContextProvider<T>? _testCtxProvider;

   /// <summary>
   /// Gets the <see cref="SqliteTestDbContextProvider{T}"/> which is created on the first access.
   /// </summary>
   protected SqliteTestDbContextProvider<T> TestCtxProvider => _testCtxProvider ??= TestCtxProviderBuilder.Build();

   private bool _isProviderConfigured;
   private readonly SqliteTestDbContextProviderBuilder<T> _testCtxProviderBuilder;
   private bool _isDisposed;

   /// <summary>
   /// Gets the <see cref="SqliteTestDbContextProviderBuilder{T}"/> which is created on the first access.
   /// </summary>
   protected SqliteTestDbContextProviderBuilder<T> TestCtxProviderBuilder
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

   /// <summary>
   /// Database context for setting up the test data.
   /// </summary>
   public T ArrangeDbContext => TestCtxProvider.ArrangeDbContext;

   /// <summary>
   /// Database context for the actual test.
   /// </summary>
   public T ActDbContext => TestCtxProvider.ActDbContext;

   /// <summary>
   /// Database context for making assertions.
   /// </summary>
   public T AssertDbContext => TestCtxProvider.AssertDbContext;

   /// <summary>
   /// Initializes a new instance of <see cref="SqliteDbContextIntegrationTests{T}"/>
   /// </summary>
   /// <param name="testOutputHelper">Output helper to use for logging.</param>
   protected SqliteDbContextIntegrationTests(ITestOutputHelper? testOutputHelper = null)
   {
      _testCtxProviderBuilder = new SqliteTestDbContextProviderBuilder<T>()
         .UseLogging(testOutputHelper);
   }

   /// <summary>
   /// Allows further configuration of the <see cref="SqliteTestDbContextProvider{T}"/>.
   /// </summary>
   /// <param name="builder">Builder for further configuration of <see cref="SqliteTestDbContextProvider{T}"/>.</param>
   protected virtual void ConfigureTestDbContextProvider(SqliteTestDbContextProviderBuilder<T> builder)
   {
   }

   /// <summary>
   /// Creates a new <see cref="DbContext"/> with a new connection.
   /// </summary>
   public T CreateDbContext()
   {
      return TestCtxProvider.CreateDbContext();
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
      return TestCtxProvider.CreateDbContext(useMasterConnection);
   }

   /// <inheritdoc />
   public virtual Task InitializeAsync()
   {
      return Task.CompletedTask;
   }

   /// <summary>
   /// Disposes of the <see cref="SqliteTestDbContextProvider{T}"/>, which disposes of the contexts, and of the master connection when the provider owns it.
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
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqliteDbContextIntegrationTests{T}.Dispose()"/>.</param>
   protected virtual void Dispose(bool disposing)
   {
      DisposeAsync(disposing).AsTask().GetAwaiter().GetResult();
   }

   /// <summary>
   /// Disposes of inner resources.
   /// </summary>
   /// <param name="disposing">Indication whether this method is being called by the method <see cref="SqliteDbContextIntegrationTests{T}.Dispose()"/>.</param>
   protected virtual async ValueTask DisposeAsync(bool disposing)
   {
      if (!disposing)
         return;

      await (_testCtxProvider?.DisposeAsync() ?? ValueTask.CompletedTask);
      _testCtxProvider = null;
   }
}
