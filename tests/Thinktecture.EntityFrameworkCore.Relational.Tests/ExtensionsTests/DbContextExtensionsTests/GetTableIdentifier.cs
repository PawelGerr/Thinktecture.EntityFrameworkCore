using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.ExtensionsTests.DbContextExtensionsTests
{
   public class GetTableIdentifier : TestBase
   {
      [Fact]
      public void Should_throw_if_context_is_null()
      {
         ((DbContext)null).Invoking(ctx => ctx.GetTableIdentifier(typeof(TestEntity))).Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_type_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         DbContextWithSchema.Invoking(ctx => ctx.GetTableIdentifier(null)).Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_type_is_unknown_to_ctx()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         DbContextWithSchema.Invoking(ctx => ctx.GetTableIdentifier(typeof(string))).Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_return_table_id_of_implicitly_specified_entity_set()
      {
         Schema = null;
         var (schema, table) = DbContextWithSchema.GetTableIdentifier(typeof(TestEntity));

         schema.Should().BeNullOrEmpty();
         table.Should().Be("TestEntity");
      }

      [Fact]
      public void Should_return_table_id_of_explicitly_specified_entity_set()
      {
         Schema = "DBC0AE92-5E63-4F0A-83E8-DDBDBD8F51C5";
         DbContextWithSchema.ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("TableName", Schema);

         var (schema, table) = DbContextWithSchema.GetTableIdentifier(typeof(TestEntity));

         schema.Should().Be(Schema);
         table.Should().Be("TableName");
      }
   }
}
