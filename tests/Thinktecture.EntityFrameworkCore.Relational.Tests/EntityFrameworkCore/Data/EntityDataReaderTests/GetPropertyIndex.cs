using System;
using System.Collections.Generic;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests
{
   public class GetPropertyIndex : IntegrationTestsBase
   {
      private readonly List<IProperty> _propertiesToRead = new List<IProperty>();
      private readonly IProperty _column1;
      private readonly IProperty _column2;

      private EntityDataReader<TestEntity> _sut;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReader<TestEntity> SUT => _sut ?? (_sut = new EntityDataReader<TestEntity>(ActDbContext, Array.Empty<TestEntity>(), _propertiesToRead));

      public GetPropertyIndex([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _column1 = ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column1));
         _column2 = ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2));
      }

      [Fact]
      public void Should_throw_if_property_is_in_propertiesToRead()
      {
         _propertiesToRead.Add(_column1);

         SUT.Invoking(sut => sut.GetPropertyIndex(_column2))
            .Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_return_index_of_the_property()
      {
         _propertiesToRead.Add(_column1);
         _propertiesToRead.Add(_column2);

         SUT.GetPropertyIndex(_column2).Should().Be(1);
      }

      protected override void Dispose(bool disposing)
      {
         _sut?.Dispose();

         base.Dispose(disposing);
      }
   }
}
