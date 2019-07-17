using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderFactoryTests
{
   public class Create : TestBase
   {
      private EntityDataReaderFactory _sut;

      private IProperty column2Property;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReaderFactory SUT => _sut ?? (_sut = new EntityDataReaderFactory());

      public Create()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();
         column2Property = DbContextWithSchema.GetEntityType<CustomTempTable>().GetProperty(nameof(CustomTempTable.Column2));
      }

      [Fact]
      public void Should_throw_if_entities_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create<TestEntity>(null, Array.Empty<IProperty>()))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_throw_if_properties_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create(Array.Empty<TestEntity>(), null))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_generate_factory_if_entities_are_empty()
      {
         var factory = SUT.Create(Array.Empty<TestEntity>(), new[] { column2Property });

         factory.Should().NotBeNull();
         factory.Read().Should().BeFalse();
      }

      [Fact]
      public void Should_generate_factory_for_provided_properties()
      {
         var entity = new CustomTempTable { Column2 = "value" };
         var factory = SUT.Create(new[] { entity }, new[] { column2Property });

         factory.FieldCount.Should().Be(1);
         factory.Read().Should().BeTrue();
         factory.GetValue(0).Should().Be("value");
      }
   }
}
