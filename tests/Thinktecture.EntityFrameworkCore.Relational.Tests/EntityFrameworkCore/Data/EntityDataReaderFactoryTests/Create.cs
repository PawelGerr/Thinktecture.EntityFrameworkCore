using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderFactoryTests
{
   public class Create : IntegrationTestsBase
   {
      private EntityDataReaderFactory? _sut;

      private readonly PropertyWithNavigations _column2Property;

      // ReSharper disable once InconsistentNaming
      private EntityDataReaderFactory SUT => _sut ??= new EntityDataReaderFactory(new PropertyGetterCache(LoggerFactory!));

      public Create(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _column2Property = new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2)), Array.Empty<INavigation>());
      }

      [Fact]
      public void Should_throw_if_context_is_null()
      {
         SUT.Invoking(sut => sut.Create(null!, Array.Empty<TestEntity>(), Array.Empty<PropertyWithNavigations>(), false))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_entities_is_null()
      {
         SUT.Invoking(sut => sut.Create<TestEntity>(ActDbContext, null!, Array.Empty<PropertyWithNavigations>(), false))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_properties_is_null()
      {
         SUT.Invoking(sut => sut.Create(ActDbContext, Array.Empty<TestEntity>(), null!, false))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_generate_factory_if_entities_are_empty()
      {
         var factory = SUT.Create(ActDbContext, Array.Empty<TestEntity>(), new[] { _column2Property }, false);

         factory.Should().NotBeNull();
         factory.Read().Should().BeFalse();
      }

      [Fact]
      public void Should_generate_factory_for_provided_properties()
      {
         var entity = new TestEntity { Column2 = "value" };
         var factory = SUT.Create(ActDbContext, new[] { entity }, new[] { _column2Property }, false);

         factory.FieldCount.Should().Be(1);
         factory.Read().Should().BeTrue();
         factory.GetValue(0).Should().Be("value");
      }
   }
}
