using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderGeneratorTests
{
   public class CreatePropertiesAccessor
   {
      private PropertiesAccessorGenerator _sut;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private PropertiesAccessorGenerator SUT => _sut ?? (_sut = new PropertiesAccessorGenerator());

      [Fact]
      public void Should_throw_if_properties_are_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.CreatePropertiesAccessor<TestEntity>(null))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_properties_are_empty()
      {
         SUT.Invoking(sut => sut.CreatePropertiesAccessor<TestEntity>(Array.Empty<PropertyInfo>()))
            .Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_throw_if_properties_dont_belong_provided_generic()
      {
         SUT.Invoking(sut => sut.CreatePropertiesAccessor<TestEntity>(new[] { typeof(CustomTempTable).GetProperties().First() }))
            .Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_generate_accessor_for_provided_properties()
      {
         var properties = typeof(CustomTempTable).GetProperties().OrderBy(p => p.Name).ToList();
         var accessor = SUT.CreatePropertiesAccessor<CustomTempTable>(properties);

         accessor.Should().NotBeNull();
         var obj = new CustomTempTable { Column1 = 42, Column2 = "foo" };

         accessor(obj, 0).Should().Be(42);
         accessor(obj, 1).Should().Be("foo");
      }

      [Fact]
      public void Should_throw_when_trying_access_property_value_that_was_not_in_properties()
      {
         var column1 = typeof(CustomTempTable).GetProperty(nameof(CustomTempTable.Column1));
         var accessor = SUT.CreatePropertiesAccessor<CustomTempTable>(new[] { column1 });

         accessor.Should().NotBeNull();
         var obj = new CustomTempTable { Column1 = 42, Column2 = "foo" };

         accessor(obj, 0).Should().Be(42);
         accessor.Invoking(a => a(obj, 1)).Should().Throw<ArgumentOutOfRangeException>();
      }
   }
}
