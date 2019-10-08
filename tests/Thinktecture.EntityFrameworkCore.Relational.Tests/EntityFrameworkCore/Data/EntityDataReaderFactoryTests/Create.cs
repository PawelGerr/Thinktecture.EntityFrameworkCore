using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderFactoryTests
{
   public class Create : IntegrationTestsBase
   {
      private EntityDataReaderFactory? _sut;

      private readonly IProperty _column2Property;

      // ReSharper disable once InconsistentNaming
      private EntityDataReaderFactory SUT => _sut ??= new EntityDataReaderFactory();

      public Create(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _column2Property = ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2));
      }

      [Fact]
      public void Should_throw_if_context_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create(null!, Array.Empty<TestEntity>(), Array.Empty<IProperty>()))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_entities_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create<TestEntity>(ActDbContext, null!, Array.Empty<IProperty>()))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_properties_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create(ActDbContext, Array.Empty<TestEntity>(), null!))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_generate_factory_if_entities_are_empty()
      {
         var factory = SUT.Create(ActDbContext, Array.Empty<TestEntity>(), new[] { _column2Property });

         factory.Should().NotBeNull();
         factory.Read().Should().BeFalse();
      }

      [Fact]
      public void Should_generate_factory_for_provided_properties()
      {
         var entity = new TestEntity { Column2 = "value" };
         var factory = SUT.Create(ActDbContext, new[] { entity }, new[] { _column2Property });

         factory.FieldCount.Should().Be(1);
         factory.Read().Should().BeTrue();
         factory.GetValue(0).Should().Be("value");
      }
   }
}
