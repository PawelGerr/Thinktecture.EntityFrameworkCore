using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit.Extensions.AssemblyFixture;

namespace Thinktecture;

public abstract class IntegrationTestsBase : IAssemblyFixture<DbContextProviderFactoryFixture>
{
   protected ILoggerFactory LoggerFactory { get; }
   protected SqliteTestDbContextProvider<TestDbContext> TestCtxProvider { get; }

   protected TestDbContext ArrangeDbContext => TestCtxProvider.ArrangeDbContext;
   protected TestDbContext ActDbContext => TestCtxProvider.ActDbContext;
   protected TestDbContext AssertDbContext => TestCtxProvider.AssertDbContext;

   protected IntegrationTestsBase(
      ITestOutputHelper testOutputHelper,
      DbContextProviderFactoryFixture providerFactoryFixture)
   {
      LoggerFactory = testOutputHelper.ToLoggerFactory();
      TestCtxProvider = providerFactoryFixture.CreateProvider(LoggerFactory);
   }
}
