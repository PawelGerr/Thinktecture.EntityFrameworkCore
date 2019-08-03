using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationAssemblyTests
{
   public abstract class DbSchemaAwareMigrationAssemblyTestsBase : TestBase
   {
      protected TestMigrationsAssembly InnerMigrationsAssembly { get; }
      protected Mock<ICurrentDbContext> CurrentCtxMock { get; }

      private DbSchemaAwareMigrationAssembly<TestMigrationsAssembly> _sut;

      [NotNull]
      protected DbSchemaAwareMigrationAssembly<TestMigrationsAssembly> SUT => _sut ?? (_sut = new DbSchemaAwareMigrationAssembly<TestMigrationsAssembly>(InnerMigrationsAssembly, CurrentCtxMock.Object));

      protected DbSchemaAwareMigrationAssemblyTestsBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         InnerMigrationsAssembly = new TestMigrationsAssembly();
         CurrentCtxMock = new Mock<ICurrentDbContext>(MockBehavior.Strict);
      }
   }
}
