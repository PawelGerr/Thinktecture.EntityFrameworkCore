using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.PropertiesProviderTests
{
   public class From
   {
      [Fact]
      public void Should_throw_if_expression_is_null()
      {
         Action action = () => EntityPropertiesProvider.From<TestEntity>(null!);
         action.Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_expression_return_constant()
      {
         Action action = () => EntityPropertiesProvider.From<TestEntity>(entity => null!);
         action.Should().Throw<NotSupportedException>();
      }

      [Fact]
      public void Should_return_empty_collection_if_no_properties_provided()
      {
         var entityType = GetEntityType<TestEntity>();
         var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => new { });
         var properties = propertiesProvider.GetPropertiesForTempTable(entityType, null, (_, _) => true);

         properties.Should().BeEmpty();
      }

      [Fact]
      public void Should_extract_property_accessor()
      {
         var entityType = GetEntityType<TestEntity>();
         var idProperty = new PropertyWithNavigations(entityType.FindProperty(nameof(TestEntity.Id))!, Array.Empty<INavigation>());
         var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => entity.Id);

         var properties = propertiesProvider.GetPropertiesForTempTable(entityType, null, (_, _) => true);
         properties.Should().HaveCount(1);
         properties.Should().Contain(idProperty);
      }

      [Fact]
      public void Should_extract_properties()
      {
         var entityType = GetEntityType<TestEntity>();
         var idProperty = new PropertyWithNavigations(entityType.FindProperty(nameof(TestEntity.Id))!, Array.Empty<INavigation>());
         var countProperty = new PropertyWithNavigations(entityType.FindProperty(nameof(TestEntity.Count))!, Array.Empty<INavigation>());
         var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => new { entity.Id, entity.Count });

         var properties = propertiesProvider.GetPropertiesForTempTable(entityType, null, (_, _) => true);
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
}
