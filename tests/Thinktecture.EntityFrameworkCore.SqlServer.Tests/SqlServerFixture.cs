namespace Thinktecture;

public class SqlServerFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly SqlServerContainerFixture? _containerFixture;
   private readonly string? _configuredConnString;

   public string ConnectionString => _containerFixture?.ConnectionString
                                     ?? _configuredConnString
                                     ?? throw new InvalidOperationException("Neither the container is used nor the connection string is configured.");

   public SqlServerFixture()
   {
      if (bool.TryParse(TestContext.Instance.Configuration["UseSqlServerContainer"], out var useContainer) && useContainer)
      {
         _containerFixture = new SqlServerContainerFixture();
      }
      else
      {
         _configuredConnString = TestContext.Instance.ConnectionString;
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
