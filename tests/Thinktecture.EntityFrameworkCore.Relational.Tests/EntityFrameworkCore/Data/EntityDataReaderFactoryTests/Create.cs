using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderFactoryTests
{
   public class Create : IDisposable
   {
      private readonly Mock<IPropertiesAccessorGenerator> _generatorMock = new Mock<IPropertiesAccessorGenerator>();
      private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

      private EntityDataReaderFactory _sut;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReaderFactory SUT => _sut ?? (_sut = new EntityDataReaderFactory(_generatorMock.Object, _memoryCache));

      [Fact]
      public void Should_throw_if_entities_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create<TestEntity>(null))
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
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<TestEntity>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((entity, i) => throw new InvalidOperationException());

         var factory = SUT.Create(Array.Empty<TestEntity>());

         factory.Should().NotBeNull();
         factory.Read().Should().BeFalse();

         _generatorMock.VerifyAll();
      }

      [Fact]
      public void Should_generate_factory_for_all_properties()
      {
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<CustomTempTable>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((entity, i) =>
                                                                                                                                 {
                                                                                                                                    if (i == 0)
                                                                                                                                       return entity.Column1;
                                                                                                                                    if (i == 1)
                                                                                                                                       return entity.Column2;

                                                                                                                                    throw new ArgumentException("out of range");
                                                                                                                                 });

         var factory = SUT.Create(new[] { new CustomTempTable { Column1 = 42, Column2 = "foo" } });

         factory.FieldCount.Should().Be(2);
         factory.Read().Should().BeTrue();
         factory.GetValue(0).Should().Be(42);
         factory.GetValue(1).Should().Be("foo");
         factory.Read().Should().BeFalse();
      }

      [Fact]
      public void Should_generate_factory_for_provided_properties()
      {
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<CustomTempTable>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((entity, i) =>
                                                                                                                                 {
                                                                                                                                    if (i == 0)
                                                                                                                                       return entity.Column1;
                                                                                                                                    if (i == 1)
                                                                                                                                       return entity.Column2;

                                                                                                                                    throw new ArgumentException("out of range");
                                                                                                                                 });
         var property = typeof(CustomTempTable).GetProperty(nameof(CustomTempTable.Column2));
         var factory = SUT.Create(new[] { new CustomTempTable { Column1 = 42, Column2 = "foo" } }, new[] { property });

         factory.FieldCount.Should().Be(1);
         factory.Read().Should().BeTrue();
         factory.GetValue(1).Should().Be("foo");
      }

      public void Dispose()
      {
         _memoryCache.Dispose();
      }
   }
}
