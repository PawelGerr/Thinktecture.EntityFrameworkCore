using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.PropertiesProviderTests;

public class Exclude
{
   [Fact]
   public void Should_throw_if_expression_is_null()
   {
      Action action = () => IEntityPropertiesProvider.Exclude<TestEntity>(null!);
      action.Should().Throw<ArgumentNullException>();
   }

   [Fact]
   public void Should_throw_if_expression_return_constant()
   {
      Action action = () => IEntityPropertiesProvider.Exclude<TestEntity>(entity => null!);
      action.Should().Throw<NotSupportedException>();
   }

   [Fact]
   public void Should_return_all_properties_if_no_properties_provided()
   {
      var entityType = GetEntityType<TestEntity>();
      var propertiesProvider = IEntityPropertiesProvider.Exclude<TestEntity>(entity => new { });
      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);

      properties.Should().HaveCount(entityType.GetFlattenedProperties().Count());
   }

   [Fact]
   public void Should_extract_all_properties_besides_the_one_specified_by_property_accessor()
   {
      var entityType = GetEntityType<TestEntity>();
      var idProperty = entityType.FindProperty(nameof(TestEntity.Id)) ?? throw new Exception("Property must no be null");

      var propertiesProvider = IEntityPropertiesProvider.Exclude<TestEntity>(entity => entity.Id);

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(entityType.GetFlattenedProperties().Count() - 1);
      properties.Should().NotContain(idProperty);
   }

   [Fact]
   public void Should_be_able_to_extract_complex_property_as_a_whole()
   {
      var entityType = GetEntityType<TestEntity>();
      var complexProperty = entityType.FindComplexProperty(nameof(TestEntity.Boundary)) ?? throw new Exception("Property must no be null");
      var propertiesOfComplexType = complexProperty.ComplexType.GetFlattenedProperties();

      var propertiesProvider = IEntityPropertiesProvider.Exclude<TestEntity>(entity => entity.Boundary);

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(entityType.GetFlattenedProperties().Count() - 2);
      properties.Should().NotContain(propertiesOfComplexType);
   }

   [Fact]
   public void Should_extract_all_properties_besides_the_ones_specified_by_expression()
   {
      var entityType = GetEntityType<TestEntity>();
      var idProperty = entityType.FindProperty(nameof(TestEntity.Id)) ?? throw new Exception("Property must no be null");
      var countProperty = entityType.FindProperty(nameof(TestEntity.Count)) ?? throw new Exception("Property must no be null");

      var propertiesProvider = IEntityPropertiesProvider.Exclude<TestEntity>(entity => new { entity.Id, entity.Count });

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(entityType.GetFlattenedProperties().Count() - 2);
      properties.Should().NotContain(idProperty);
      properties.Should().NotContain(countProperty);
   }

   [Fact]
   public void Should_handle_properties_from_base_classes()
   {
      var entityType = GetEntityType<TestEntityWithBaseClass>();
      var idProperty = entityType.FindProperty(nameof(TestEntityWithBaseClass.Id)) ?? throw new Exception("Property must no be null");
      var baseProperty = entityType.FindProperty(nameof(TestEntityWithBaseClass.Base_A)) ?? throw new Exception("Property must no be null");

      var propertiesProvider = IEntityPropertiesProvider.Exclude<TestEntityWithBaseClass>(entity => new { entity.Id, entity.Base_A });

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(entityType.GetFlattenedProperties().Count() - 2);
      properties.Should().NotContain(idProperty);
      properties.Should().NotContain(baseProperty);
   }

   private static IEntityType GetEntityType<T>()
   {
      var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite("DataSource=:memory:").Options;
      return new TestDbContext(options).Model.GetEntityType(typeof(T));
   }
}
