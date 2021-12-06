using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DefaultSchemaModelCustomizerTests;

public class Customize : IntegrationTestsBase
{
   public Customize(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      ConfigureOptionsBuilder = builder =>
                                {
                                   builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.Register(typeof(ModelCustomizer), typeof(ModelCustomizer), ServiceLifetime.Singleton));
                                   builder.ReplaceService<IModelCustomizer, DefaultSchemaModelCustomizer<ModelCustomizer>>();
                                };
   }

   [Fact]
   public void Should_set_schema_if_entity_schema_is_null()
   {
      Schema = "BA3C32B0-D7EC-422C-AE3B-3206E7D67735";

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be(Schema);
   }

   [Fact]
   public void Should_set_schema_if_query_type_schema_is_null()
   {
      Schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

      ActDbContext.Model.FindEntityType(typeof(TestQuery))!.GetSchema().Should().Be(Schema);
   }

   [Fact]
   public void Should_set_schema_if_dbFunction_schema_is_null()
   {
      Schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

      ActDbContext.Model.GetDbFunctions()
                  .Single(f => f.MethodInfo!.Name == nameof(DbContextWithSchema.TestDbFunction))
                  .Schema.Should().Be(Schema);
   }

   [Fact]
   public void Should_not_change_schema_if_entity_schema_is_set_already()
   {
      Schema = "4BA05B95-7FEA-4F32-A1E0-ACA9CB486EB9";
      ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be("Schema");
   }

   [Fact]
   public void Should_not_reset_schema_if_entity_schema_is_set_already()
   {
      Schema = null;
      ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be("Schema");
   }
}
