namespace Thinktecture;

public class NpgsqlFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly NpgsqlContainerFixture? _containerFixture;

   public string ConnectionString => _containerFixture?.ConnectionString
                                     ?? field
                                     ?? throw new InvalidOperationException("Neither the container is used nor the connection string is configured.");

   public NpgsqlFixture()
   {
      if (bool.TryParse(TestContext.Instance.Configuration["UsePostgreSqlContainer"], out var useContainer) && useContainer)
      {
         _containerFixture = new NpgsqlContainerFixture();
      }
      else
      {
         ConnectionString = TestContext.Instance.ConnectionString;
      }
   }

   public async Task InitializeAsync()
   {
      if (_containerFixture is not null)
         await _containerFixture.InitializeAsync();
   }

   public void Dispose()
   {
      _containerFixture?.Dispose();
   }

   async Task IAsyncLifetime.DisposeAsync()
   {
      await DisposeAsync();
   }

   public async ValueTask DisposeAsync()
   {
      if (_containerFixture is not null)
         await _containerFixture.DisposeAsync();
   }
}
