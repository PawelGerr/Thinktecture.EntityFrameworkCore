using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DefaultSchemaModelCustomizerTests;

public class Customize : IntegrationTestsBase
{
   private string? _schema;

   public Customize(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   protected override void ConfigureTestDbContextProvider(SqliteTestDbContextProviderBuilder<DbContextWithSchema> builder)
   {
      base.ConfigureTestDbContextProvider(builder);

      builder.ConfigureOptions(optionsBuilder =>
                               {
                                  optionsBuilder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension =>
                                                                                                           {
                                                                                                              extension.Register(typeof(ModelCustomizer), typeof(ModelCustomizer), ServiceLifetime.Singleton);
                                                                                                              return extension;
                                                                                                           });
                                  optionsBuilder.ReplaceService<IModelCustomizer, DefaultSchemaModelCustomizer<ModelCustomizer>>();
                               });

      if (_schema is not null)
      {
         builder.UseContextFactory(options => new DbContextWithSchema(options, _schema))
                .DisableModelCache();
      }
   }

   [Fact]
   public void Should_set_schema_if_entity_schema_is_null()
   {
      _schema = "BA3C32B0-D7EC-422C-AE3B-3206E7D67735";

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be(_schema);
   }

   [Fact]
   public void Should_set_schema_if_view_schema_is_null()
   {
      _schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

      ActDbContext.Model.FindEntityType(typeof(TestQuery))!.GetViewSchema().Should().Be(_schema);
   }

   [Fact]
   public void Should_set_schema_if_dbFunction_schema_is_null()
   {
      _schema = "E2FBA720-E24C-46C9-B326-46C3C91707F5";

      ActDbContext.Model.GetDbFunctions()
                  .Single(f => f.MethodInfo!.Name == nameof(DbContextWithSchema.TestDbFunction))
                  .Schema.Should().Be(_schema);
   }

   [Fact]
   public void Should_not_change_schema_if_entity_schema_is_set_already()
   {
      _schema = "4BA05B95-7FEA-4F32-A1E0-ACA9CB486EB9";
      ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be("Schema");
   }

   [Fact]
   public void Should_not_reset_schema_if_entity_schema_is_set_already()
   {
      _schema = null;
      ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("Table", "Schema");

      ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetSchema().Should().Be("Schema");
   }
}
