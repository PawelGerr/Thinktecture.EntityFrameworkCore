using System;
using FluentAssertions;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.PropertiesProviderTests
{
   public class From
   {
      [Fact]
      public void Should_throw_if_expression_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         Action action = () => EntityMembersProvider.From<TestEntity>(null!);
         action.Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_no_properties_provided()
      {
         Action action = () => EntityMembersProvider.From<TestEntity>(entity => new { });
         action.Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_throw_if_expression_return_constant()
      {
         Action action = () => EntityMembersProvider.From<TestEntity>(entity => null!);
         action.Should().Throw<NotSupportedException>();
      }

      [Fact]
      public void Should_extract_property_accessor()
      {
         var propertiesProvider = EntityMembersProvider.From<TestEntity>(entity => entity.Id);

         var properties = propertiesProvider.GetMembers();
         properties.Should().HaveCount(1);
         var idProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Id)) ?? throw new Exception($"Property {nameof(TestEntity.Id)} not found.");
         properties.Should().Contain(idProperty);
      }

      [Fact]
      public void Should_extract_properties()
      {
         var propertiesProvider = EntityMembersProvider.From<TestEntity>(entity => new { entity.Id, entity.Count });

         var properties = propertiesProvider.GetMembers();
         properties.Should().HaveCount(2);
         var idProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Id)) ?? throw new Exception($"Property {nameof(TestEntity.Id)} not found.");
         var countProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Count)) ?? throw new Exception($"Property {nameof(TestEntity.Count)} not found.");
         properties.Should().Contain(idProperty);
         properties.Should().Contain(countProperty);
      }
   }
}
