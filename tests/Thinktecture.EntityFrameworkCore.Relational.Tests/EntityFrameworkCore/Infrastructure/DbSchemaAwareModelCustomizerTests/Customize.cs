using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DbSchemaAwareModelCustomizerTests
{
   public class Customize : TestBase
   {
      public Customize()
      {
         OptionBuilder.ReplaceService<IModelCustomizer, DbSchemaAwareModelCustomizer>();
      }

      [Fact]
      public void Should_set_schema_if_entity_schema_is_null()
      {
         Schema = "BA3C32B0-D7EC-422C-AE3B-3206E7D67735";

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be(Schema);
      }

      [Fact]
      public void Should_not_change_schema_if_entity_schema_is_set_already()
      {
         Schema = "4BA05B95-7FEA-4F32-A1E0-ACA9CB486EB9";
         DbContextWithSchema.ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be("Schema");
      }

      [Fact]
      public void Should_not_reset_schema_if_entity_schema_is_set_already()
      {
         Schema = null;
         DbContextWithSchema.ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be("Schema");
      }
   }
}
