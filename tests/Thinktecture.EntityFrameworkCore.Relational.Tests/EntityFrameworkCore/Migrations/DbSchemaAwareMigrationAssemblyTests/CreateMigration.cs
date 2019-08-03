using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationAssemblyTests
{
   public class CreateMigration : DbSchemaAwareMigrationAssemblyTestsBase
   {
      public CreateMigration([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_throw_when_schema_type_is_null()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithSchema("Schema1"));

         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.CreateMigration(null, "DummyProvider"))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_when_active_provider_is_null()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithSchema("Schema1"));

         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.CreateMigration(typeof(MigrationWithSchema).GetTypeInfo(), null))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_create_schema_aware_migration_having_schema_aware_ctx()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithSchema("Schema1"));

         var migration = SUT.CreateMigration(typeof(MigrationWithSchema).GetTypeInfo(), "DummyProvider");

         migration.Should().NotBeNull();
         migration.Should().BeOfType<MigrationWithSchema>();
      }

      [Fact]
      public void Should_throw_when_creating_schema_aware_migration_having_schema_unaware_ctx()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithoutSchema());

         SUT.Invoking(sut => sut.CreateMigration(typeof(MigrationWithSchema).GetTypeInfo(), "DummyProvider"))
            .Should().Throw<ArgumentException>().WithMessage($@"For instantiation of schema-aware migration of type '{nameof(MigrationWithSchema)}' the database context of type '{typeof(DbContextWithoutSchema).FullName}' has to implement the interface '{nameof(IDbContextSchema)}'.
Parameter name: migrationClass");
      }

      [Fact]
      public void Should_delegate_schema_unaware_migration_to_inner_migrationassembly_having_schema_aware_context()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithSchema("Schema1"));
         var migration = new MigrationWithoutSchema();
         InnerMigrationsAssembly.Mock.Setup(a => a.CreateMigration(It.IsAny<TypeInfo>(), It.IsAny<string>())).Returns(migration);

         var createMigration = SUT.CreateMigration(typeof(MigrationWithoutSchema).GetTypeInfo(), "DummyProvider");

         createMigration.Should().Be(migration);
         InnerMigrationsAssembly.Mock.VerifyAll();
      }

      [Fact]
      public void Should_create_schema_unaware_migration_having_schema_unaware_ctx()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithoutSchema());
         var migration = new MigrationWithoutSchema();
         InnerMigrationsAssembly.Mock.Setup(a => a.CreateMigration(It.IsAny<TypeInfo>(), It.IsAny<string>())).Returns(migration);

         var createMigration = SUT.CreateMigration(typeof(MigrationWithoutSchema).GetTypeInfo(), "DummyProvider");

         createMigration.Should().Be(migration);
         InnerMigrationsAssembly.Mock.VerifyAll();
      }
   }
}
