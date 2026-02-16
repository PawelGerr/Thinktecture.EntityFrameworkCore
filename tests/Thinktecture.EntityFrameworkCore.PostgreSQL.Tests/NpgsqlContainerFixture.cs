using Testcontainers.PostgreSql;

namespace Thinktecture;

public class NpgsqlContainerFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly PostgreSqlContainer _postgreSqlContainer;
   private bool _isDisposed;

   public string ConnectionString => _postgreSqlContainer.GetConnectionString();

   public NpgsqlContainerFixture()
   {
      _postgreSqlContainer = BuildContainer();
   }

   private static PostgreSqlContainer BuildContainer()
   {
      return new PostgreSqlBuilder("postgres:17")
             .WithCleanUp(true)
             .Build();
   }

   public async Task InitializeAsync()
   {
      await _postgreSqlContainer.StartAsync();
   }

   public void Dispose()
   {
      DisposeAsync().AsTask().GetAwaiter().GetResult();
   }

   async Task IAsyncLifetime.DisposeAsync()
   {
      await DisposeAsync();
   }

   public async ValueTask DisposeAsync()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      await _postgreSqlContainer.StopAsync();
      await _postgreSqlContainer.DisposeAsync();
   }
}
