using Testcontainers.MySql;

namespace Thinktecture;

public class MySqlContainerFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly MySqlContainer? _mySqlContainer;
   private bool _isDisposed;

   // Pinned LTS image; the server version used by the EF options must match it.
   public const string Image = "mysql:8.4";

   public static readonly MySqlServerVersion ServerVersion = new(new Version(8, 4, 0));

   public string ConnectionString => _mySqlContainer?.GetConnectionString()
                                     ?? field
                                     ?? throw new InvalidOperationException("Neither the container is used nor the connection string is configured.");

   public MySqlContainerFixture()
   {
      if (bool.TryParse(TestContext.Instance.Configuration["UseMySqlContainer"], out var useContainer) && useContainer)
      {
         _mySqlContainer = BuildContainer();
      }
      else
      {
         ConnectionString = TestContext.Instance.ConnectionString;
      }
   }

   private static MySqlContainer BuildContainer()
   {
      return new MySqlBuilder(Image)
             .WithCommand("--local-infile=1")
             .WithCleanUp(true)
             .Build();
   }

   public async Task InitializeAsync()
   {
      if (_mySqlContainer is not null)
         await _mySqlContainer.StartAsync();
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

      if (_mySqlContainer is null)
         return;

      await _mySqlContainer.StopAsync();
      await _mySqlContainer.DisposeAsync();
   }
}
