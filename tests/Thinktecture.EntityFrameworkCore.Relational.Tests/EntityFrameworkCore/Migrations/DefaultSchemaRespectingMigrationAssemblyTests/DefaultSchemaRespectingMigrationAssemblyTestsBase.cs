using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests;

public abstract class DefaultSchemaRespectingMigrationAssemblyTestsBase : IntegrationTestsBase
{
   private TestMigrationsAssembly InnerMigrationsAssembly { get; }
   protected ICurrentDbContext CurrentCtxMock { get; }
   protected IMigrationOperationSchemaSetter SchemaSetterMock { get; }
   protected IServiceCollection Services { get; }

   private DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>? _sut;

   protected DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> SUT => _sut ??= new DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>(InnerMigrationsAssembly, SchemaSetterMock, CurrentCtxMock, Services.BuildServiceProvider());

   protected DefaultSchemaRespectingMigrationAssemblyTestsBase(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      InnerMigrationsAssembly = new TestMigrationsAssembly();
      CurrentCtxMock = Substitute.For<ICurrentDbContext>();
      SchemaSetterMock = Substitute.For<IMigrationOperationSchemaSetter>();
      Services = new ServiceCollection();
   }
}
