using System;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DbSchemaAwareModelCacheKeyFactoryTests
{
   public class Create : TestBase
   {
      private DbSchemaAwareModelCacheKeyFactory _sut;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private DbSchemaAwareModelCacheKeyFactory SUT => _sut ?? (_sut = new DbSchemaAwareModelCacheKeyFactory());

      [Fact]
      public void Should_throw_if_ctx_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         SUT.Invoking(sut => sut.Create(null))
            .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_create_class_with_correct_equals_implementation_for_schema_aware_ctx()
      {
         var cacheKey1 = SUT.Create(CreateContextWithSchema("Schema1"));
         var anotherCacheKey1 = SUT.Create(CreateContextWithSchema("Schema1"));
         var cacheKey2 = SUT.Create(CreateContextWithSchema("Schema2"));

         cacheKey1.Equals(anotherCacheKey1).Should().BeTrue();
         cacheKey1.Equals(cacheKey2).Should().BeFalse();
      }

      [Fact]
      public void Should_create_class_with_correct_gethashcode_implementation_for_schema_aware_ctx()
      {
         var cacheKey1 = SUT.Create(CreateContextWithSchema("Schema1"));
         var anotherCacheKey1 = SUT.Create(CreateContextWithSchema("Schema1"));
         var cacheKey2 = SUT.Create(CreateContextWithSchema("Schema2"));

         cacheKey1.GetHashCode().Should().Be(anotherCacheKey1.GetHashCode());
         cacheKey1.GetHashCode().Should().NotBe(cacheKey2.GetHashCode());
      }

      [Fact]
      public void Should_create_class_with_correct_equals_implementation_for_schema_unaware_ctx()
      {
         var cacheKey1 = SUT.Create(CreateContextWithoutSchema());
         var cacheKey2 = SUT.Create(CreateContextWithoutSchema());

         cacheKey1.Equals(cacheKey2).Should().BeTrue();
      }

      [Fact]
      public void Should_create_class_with_correct_gethashcode_implementation_for_schema_unaware_ctx()
      {
         var cacheKey1 = SUT.Create(CreateContextWithoutSchema());
         var cacheKey2 = SUT.Create(CreateContextWithoutSchema());

         cacheKey1.GetHashCode().Should().Be(cacheKey2.GetHashCode());
      }
   }
}
