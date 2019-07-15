using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationAssemblyTests
{
   public class CreateMigration : DbSchemaAwareMigrationAssemblyTestsBase
   {
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
            .Should().Throw<ArgumentException>().WithMessage(@"For instantiation of database schema aware migration of type 'MigrationWithSchema' the database context of type 'DbContextWithoutSchema' has to implement the interface IDbContextSchema.
Parameter name: migrationClass");
      }

      [Fact]
      public void Should_create_schema_unaware_migration_having_schema_aware_ctx()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithSchema("Schema1"));

         var migration = SUT.CreateMigration(typeof(MigrationWithoutSchema).GetTypeInfo(), "DummyProvider");

         migration.Should().NotBeNull();
         migration.Should().BeOfType<MigrationWithoutSchema>();
      }

      [Fact]
      public void Should_create_schema_unaware_migration_having_schema_unaware_ctx()
      {
         CurrentCtxMock.Setup(c => c.Context).Returns(CreateContextWithoutSchema());

         var migration = SUT.CreateMigration(typeof(MigrationWithoutSchema).GetTypeInfo(), "DummyProvider");

         migration.Should().NotBeNull();
         migration.Should().BeOfType<MigrationWithoutSchema>();
      }
   }
}
