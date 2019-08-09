using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DefaultSchemaModelCustomizerTests
{
   public class Customize : TestBase
   {
      public Customize([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         OptionBuilder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.Add(ServiceDescriptor.Singleton(typeof(ModelCustomizer), typeof(ModelCustomizer))));
         OptionBuilder.ReplaceService<IModelCustomizer, DefaultSchemaModelCustomizer<ModelCustomizer>>();
      }

      [Fact]
      public void Should_set_schema_if_entity_schema_is_null()
      {
         Schema = "BA3C32B0-D7EC-422C-AE3B-3206E7D67735";

         DbContextWithSchema.Model.FindEntityType(typeof(TestEntity)).Relational().Schema.Should().Be(Schema);
      }

      [Fact]
      public void Should_set_schema_if_query_type_schema_is_null()
      {
         Schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

         DbContextWithSchema.Model.FindEntityType(typeof(TestQuery)).Relational().Schema.Should().Be(Schema);
      }

      [Fact]
      public void Should_set_schema_if_dbFunction_schema_is_null()
      {
         Schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

         DbContextWithSchema.Model.Relational().DbFunctions
                            .Single(f => f.MethodInfo.Name == nameof(DbContextWithSchema.TestDbFunction))
                            .Schema.Should().Be(Schema);
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
