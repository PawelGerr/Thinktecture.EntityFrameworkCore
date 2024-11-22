using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit.Extensions.AssemblyFixture;

[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]

namespace Thinktecture;

public class DbContextProviderFactoryFixture : IAsyncLifetime
{
   private readonly SqliteTestDbContextProviderFactory<TestDbContext> _factory;

   public DbContextProviderFactoryFixture()
   {
      _factory = ConfigureBuilder().BuildFactory();
   }

   private static SqliteTestDbContextProviderBuilder<TestDbContext> ConfigureBuilder()
   {
      return new SqliteTestDbContextProviderBuilder<TestDbContext>()
             .UseMigrationExecutionStrategy(IMigrationExecutionStrategy.Migrations)
             .UseMigrationLogLevel(LogLevel.Warning)
             .ConfigureSqliteOptions(optionsBuilder => optionsBuilder.AddBulkOperationSupport()
                                                                     .AddWindowFunctionsSupport());
   }

   public SqliteTestDbContextProvider<TestDbContext> CreateProvider(ILoggerFactory loggerFactory)
   {
      return _factory.Create(loggerFactory);
   }

   public async Task InitializeAsync()
   {
      await _factory.InitializeAsync();
   }

   public async Task DisposeAsync()
   {
      await _factory.DisposeAsync();
   }
}
