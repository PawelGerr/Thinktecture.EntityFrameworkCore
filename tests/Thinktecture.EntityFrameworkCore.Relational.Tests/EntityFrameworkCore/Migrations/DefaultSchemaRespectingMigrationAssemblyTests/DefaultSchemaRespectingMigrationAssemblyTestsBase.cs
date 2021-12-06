using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests;

public abstract class DefaultSchemaRespectingMigrationAssemblyTestsBase : IntegrationTestsBase
{
   private TestMigrationsAssembly InnerMigrationsAssembly { get; }
   protected Mock<ICurrentDbContext> CurrentCtxMock { get; }
   protected Mock<IMigrationOperationSchemaSetter> SchemaSetterMock { get; }
   protected IServiceCollection Services { get; }

   private DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>? _sut;

   protected DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> SUT => _sut ??= new DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>(InnerMigrationsAssembly, SchemaSetterMock.Object, CurrentCtxMock.Object, Services.BuildServiceProvider());

   protected DefaultSchemaRespectingMigrationAssemblyTestsBase(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      InnerMigrationsAssembly = new TestMigrationsAssembly();
      CurrentCtxMock = new Mock<ICurrentDbContext>(MockBehavior.Strict);
      SchemaSetterMock = new Mock<IMigrationOperationSchemaSetter>();
      Services = new ServiceCollection();
   }
}