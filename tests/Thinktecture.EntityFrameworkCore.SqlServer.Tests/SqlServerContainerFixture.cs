using Testcontainers.MsSql;

namespace Thinktecture;

public class SqlServerContainerFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly MsSqlContainer _sqlServerContainer;
   private bool _isDisposed;

   public string ConnectionString => _sqlServerContainer.GetConnectionString();

   public SqlServerContainerFixture()
   {
      _sqlServerContainer = BuildContainer();
   }

   private static MsSqlContainer BuildContainer()
   {
      return new MsSqlBuilder()
             .WithImage("mcr.microsoft.com/mssql/server:2022-CU13-ubuntu-22.04")
             .WithPassword($"P@sswo0d01_{Guid.NewGuid()}")
             .WithCleanUp(true)
             .Build();
   }

   public async Task InitializeAsync()
   {
      await _sqlServerContainer.StartAsync();
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

      await _sqlServerContainer.StopAsync();
      await _sqlServerContainer.DisposeAsync();
   }
}
