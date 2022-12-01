using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.PropertiesProviderTests;

public class Include
{
   [Fact]
   public void Should_throw_if_expression_is_null()
   {
      Action action = () => IEntityPropertiesProvider.Include<TestEntity>(null!);
      action.Should().Throw<ArgumentNullException>();
   }

   [Fact]
   public void Should_throw_if_expression_return_constant()
   {
      Action action = () => IEntityPropertiesProvider.Include<TestEntity>(entity => null!);
      action.Should().Throw<NotSupportedException>();
   }

   [Fact]
   public void Should_return_empty_collection_if_no_properties_provided()
   {
      var entityType = GetEntityType<TestEntity>();
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => new { });
      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);

      properties.Should().BeEmpty();
   }

   [Fact]
   public void Should_extract_property_accessor()
   {
      var entityType = GetEntityType<TestEntity>();
      var idProperty = entityType.FindProperty(nameof(TestEntity.Id));
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => entity.Id);

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(1);
      properties.Should().Contain(idProperty);
   }

   [Fact]
   public void Should_extract_properties()
   {
      var entityType = GetEntityType<TestEntity>();
      var idProperty = entityType.FindProperty(nameof(TestEntity.Id));
      var countProperty = entityType.FindProperty(nameof(TestEntity.Count));
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => new { entity.Id, entity.Count });

      var properties = propertiesProvider.GetPropertiesForTempTable(entityType);
      properties.Should().HaveCount(2);
      properties.Should().Contain(idProperty);
      properties.Should().Contain(countProperty);
   }

   private static IEntityType GetEntityType<T>()
   {
      var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite("DataSource=:memory:").Options;
      return new TestDbContext(options).Model.GetEntityType(typeof(T));
   }
}
