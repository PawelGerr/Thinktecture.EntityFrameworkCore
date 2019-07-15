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
      public delegate object GetValue<in T>(T entity, int index);

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
      public void Should_use_propertyAccessor_from_cache()
      {
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<TestEntity>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((entity, i) => throw new InvalidOperationException());

         SUT.Create(Array.Empty<TestEntity>());
         SUT.Create(Array.Empty<TestEntity>());

         _generatorMock.Verify(g => g.CreatePropertiesAccessor<TestEntity>(It.IsAny<IReadOnlyList<PropertyInfo>>()), Times.Once);
      }

      [Fact]
      public void Should_generate_factory_for_all_properties()
      {
         var delegateMock = new Mock<GetValue<CustomTempTable>>();
         delegateMock.Setup(_ => _(It.IsAny<CustomTempTable>(), It.IsAny<int>())).Returns(null);
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<CustomTempTable>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((e, i) => delegateMock.Object(e, i));

         var entity = new CustomTempTable();
         var factory = SUT.Create(new[] { entity });

         factory.FieldCount.Should().Be(2);
         factory.Read().Should().BeTrue();

         factory.GetValue(0).Should().BeNull();
         delegateMock.Verify(_ => _(entity, 0), Times.Once);

         factory.GetValue(1).Should().BeNull();
         delegateMock.Verify(_ => _(entity, 1), Times.Once);

         factory.Read().Should().BeFalse();

         delegateMock.Verify(_ => _(It.IsAny<CustomTempTable>(), It.IsAny<int>()), Times.Exactly(2));
      }

      [Fact]
      public void Should_generate_factory_for_provided_properties()
      {
         var delegateMock = new Mock<GetValue<CustomTempTable>>();
         delegateMock.Setup(_ => _(It.IsAny<CustomTempTable>(), It.IsAny<int>())).Returns(null);
         _generatorMock.Setup(g => g.CreatePropertiesAccessor<CustomTempTable>(It.IsAny<IReadOnlyList<PropertyInfo>>())).Returns((e, i) => delegateMock.Object(e, i));

         var property = typeof(CustomTempTable).GetProperty(nameof(CustomTempTable.Column2));
         var entity = new CustomTempTable();
         var factory = SUT.Create(new[] { entity }, new[] { property });

         factory.FieldCount.Should().Be(1);
         factory.Read().Should().BeTrue();
         factory.GetValue(1).Should().BeNull();
         delegateMock.Verify(_ => _(entity, 1), Times.Once);
      }

      public void Dispose()
      {
         _memoryCache.Dispose();
      }
   }
}
