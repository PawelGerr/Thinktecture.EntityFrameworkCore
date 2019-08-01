using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Moq;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationAssemblyTests
{
   public abstract class DbSchemaAwareMigrationAssemblyTestsBase : TestBase
   {
      protected Mock<ICurrentDbContext> CurrentCtxMock { get; }
      protected Mock<IDbContextOptions> OptionsMock { get; }
      protected Mock<IMigrationsIdGenerator> IdGeneratorMock { get; }
      protected Mock<IDiagnosticsLogger<DbLoggerCategory.Migrations>> LoggerMock { get; }

      private DbSchemaAwareMigrationAssembly _sut;

      [NotNull]
      protected DbSchemaAwareMigrationAssembly SUT => _sut ?? (_sut = new DbSchemaAwareMigrationAssembly(CurrentCtxMock.Object, OptionsMock.Object, IdGeneratorMock.Object, LoggerMock.Object));

      protected DbSchemaAwareMigrationAssemblyTestsBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         CurrentCtxMock = new Mock<ICurrentDbContext>(MockBehavior.Strict);
         OptionsMock = new Mock<IDbContextOptions>(MockBehavior.Strict);
         IdGeneratorMock = new Mock<IMigrationsIdGenerator>(MockBehavior.Strict);
         LoggerMock = new Mock<IDiagnosticsLogger<DbLoggerCategory.Migrations>>(MockBehavior.Strict);

         var optionsExtensionMock = new Mock<RelationalOptionsExtension>();
         OptionsMock.Setup(o => o.Extensions).Returns(new List<IDbContextOptionsExtension> { optionsExtensionMock.Object });
      }
   }
}
