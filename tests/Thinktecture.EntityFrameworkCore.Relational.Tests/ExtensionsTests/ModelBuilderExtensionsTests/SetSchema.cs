using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Thinktecture.ExtensionsTests.ModelBuilderExtensionsTests
{
   public class SetSchema : TestBase
   {
      [Fact]
      public void Should_throw_if_builder_is_null()
      {
         ((ModelBuilder)null).Invoking(b => b.SetSchema("schema")).Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_set_schema()
      {
         DbContextWithRandomSchema.ConfigureModel = builder => builder.SetSchema("schema");

         DbContextWithRandomSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be("schema");
         DbContextWithRandomSchema.Model.FindEntityType(typeof(TestQuery)).Relational().Schema.Should().Be("schema");
      }

      [Fact]
      public void Should_reset_schema()
      {
         DbContextWithRandomSchema.ConfigureModel = builder =>
                                                    {
                                                       builder.Entity<TestEntity>().ToTable("table", "schema");
                                                       builder.Query<TestQuery>().ToView("view", "schema");
                                                       builder.SetSchema(null);
                                                    };

         DbContextWithRandomSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().BeNull();
         DbContextWithRandomSchema.Model.FindEntityType(typeof(TestQuery)).Relational().Schema.Should().BeNull();
      }
   }
}
