using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Thinktecture;

public class SqlServerContainerFixture : IDisposable, IAsyncDisposable, IAsyncLifetime
{
   private readonly MsSqlTestcontainer _sqlServerContainer;
   private bool _isDisposed;

   public string ConnectionString => $"{_sqlServerContainer.ConnectionString};TrustServerCertificate=true;";

   public SqlServerContainerFixture()
   {
      _sqlServerContainer = BuildContainer();
   }

   private static MsSqlTestcontainer BuildContainer()
   {
      // https: //github.com/testcontainers/testcontainers-dotnet/issues/750#issuecomment-1412257694
#pragma warning disable 618
      return new TestcontainersBuilder<MsSqlTestcontainer>()
#pragma warning restore 618
             .WithDatabase(new MsSqlTestcontainerConfiguration("mcr.microsoft.com/mssql/server:2022-latest")
                           {
                              Password = $"P@sswo0d01_{Guid.NewGuid()}"
                           })
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
