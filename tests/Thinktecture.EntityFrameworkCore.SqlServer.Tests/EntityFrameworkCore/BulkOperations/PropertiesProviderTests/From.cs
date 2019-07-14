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
         Action action = () => PropertiesProvider.From<TestEntity>(null);
         action.Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_no_properties_provided()
      {
         Action action = () => PropertiesProvider.From<TestEntity>(entity => new { });
         action.Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_throw_if_expression_return_constant()
      {
         Action action = () => PropertiesProvider.From<TestEntity>(entity => null);
         action.Should().Throw<NotSupportedException>();
      }

      [Fact]
      public void Should_extract_property_accessor()
      {
         var propertiesProvider = PropertiesProvider.From<TestEntity>(entity => entity.Id);

         var properties = propertiesProvider.GetProperties();
         properties.Should().HaveCount(1);
         properties.Should().Contain(typeof(TestEntity).GetProperty(nameof(TestEntity.Id)));
      }

      [Fact]
      public void Should_extract_properties()
      {
         var propertiesProvider = PropertiesProvider.From<TestEntity>(entity => new { entity.Id, entity.Count });

         var properties = propertiesProvider.GetProperties();
         properties.Should().HaveCount(2);
         properties.Should().Contain(typeof(TestEntity).GetProperty(nameof(TestEntity.Id)));
         properties.Should().Contain(typeof(TestEntity).GetProperty(nameof(TestEntity.Count)));
      }
   }
}
