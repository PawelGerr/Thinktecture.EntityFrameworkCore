using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.ExtensionsTests.DbContextExtensionsTests
{
   public class GetTableIdentifier : TestBase
   {
      [Fact]
      public void Should_throw_if_type_is_null()
      {
         ((IEntityType)null).Invoking(t => t.GetTableIdentifier()).Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_return_table_id_of_implicitly_specified_entity_set()
      {
         Schema = null;
         var (schema, table) = DbContextWithSchema.GetEntityType<TestEntity>().GetTableIdentifier();

         schema.Should().BeNullOrEmpty();
         table.Should().Be("TestEntity");
      }

      [Fact]
      public void Should_return_table_id_of_explicitly_specified_entity_set()
      {
         Schema = "DBC0AE92-5E63-4F0A-83E8-DDBDBD8F51C5";
         DbContextWithSchema.ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("TableName", Schema);

         var (schema, table) = DbContextWithSchema.GetEntityType<TestEntity>().GetTableIdentifier();

         schema.Should().Be(Schema);
         table.Should().Be("TableName");
      }
   }
}
