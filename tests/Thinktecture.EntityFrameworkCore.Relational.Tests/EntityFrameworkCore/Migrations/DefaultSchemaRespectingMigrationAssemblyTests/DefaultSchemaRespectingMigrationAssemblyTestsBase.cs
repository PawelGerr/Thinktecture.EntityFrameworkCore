using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests
{
   public abstract class DefaultSchemaRespectingMigrationAssemblyTestsBase : IntegrationTestsBase
   {
      private TestMigrationsAssembly InnerMigrationsAssembly { get; }
      protected Mock<ICurrentDbContext> CurrentCtxMock { get; }
      protected Mock<IMigrationOperationSchemaSetter> SchemaSetterMock { get; }
      protected IServiceCollection Services { get; }

      private DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> _sut;

      [NotNull]
      protected DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> SUT => _sut ?? (_sut = new DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>(InnerMigrationsAssembly, SchemaSetterMock.Object, CurrentCtxMock.Object, Services.BuildServiceProvider()));

      protected DefaultSchemaRespectingMigrationAssemblyTestsBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         InnerMigrationsAssembly = new TestMigrationsAssembly();
         CurrentCtxMock = new Mock<ICurrentDbContext>(MockBehavior.Strict);
         SchemaSetterMock = new Mock<IMigrationOperationSchemaSetter>();
         Services = new ServiceCollection();
      }
   }
}
