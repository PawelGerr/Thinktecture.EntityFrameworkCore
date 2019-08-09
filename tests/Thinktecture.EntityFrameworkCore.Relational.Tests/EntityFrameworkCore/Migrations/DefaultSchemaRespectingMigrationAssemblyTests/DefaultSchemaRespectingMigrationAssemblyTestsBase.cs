using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DefaultSchemaRespectingMigrationAssemblyTests
{
   public abstract class DefaultSchemaRespectingMigrationAssemblyTestsBase : TestBase
   {
      protected TestMigrationsAssembly InnerMigrationsAssembly { get; }
      protected Mock<ICurrentDbContext> CurrentCtxMock { get; }
      protected Mock<IMigrationOperationSchemaSetter> SchemaSetterMock { get; }

      private DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> _sut;

      [NotNull]
      protected DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly> SUT => _sut ?? (_sut = new DefaultSchemaRespectingMigrationAssembly<TestMigrationsAssembly>(InnerMigrationsAssembly, SchemaSetterMock.Object, CurrentCtxMock.Object));

      protected DefaultSchemaRespectingMigrationAssemblyTestsBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         InnerMigrationsAssembly = new TestMigrationsAssembly();
         CurrentCtxMock = new Mock<ICurrentDbContext>(MockBehavior.Strict);
         SchemaSetterMock = new Mock<IMigrationOperationSchemaSetter>();
      }
   }
}
