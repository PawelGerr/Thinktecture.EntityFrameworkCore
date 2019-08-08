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
   public class Properties : TestBase
   {
      private readonly List<IProperty> _propertiesToRead = new List<IProperty>();
      private readonly IProperty _column1;
      private readonly IProperty _column2;

      private EntityDataReader<TestEntity> _sut;

      public Properties([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _column1 = DbContextWithSchema.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column1));
         _column2 = DbContextWithSchema.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2));
      }

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReader<TestEntity> SUT => _sut ?? (_sut = new EntityDataReader<TestEntity>(DbContextWithSchema, Array.Empty<TestEntity>(), _propertiesToRead));

      [Fact]
      public void Should_return_propertiesToRead()
      {
         _propertiesToRead.Add(_column1);
         _propertiesToRead.Add(_column2);

         SUT.Properties.Should().HaveCount(2);
         SUT.Properties.Should().Contain(_column1);
         SUT.Properties.Should().Contain(_column2);
      }

      public override void Dispose()
      {
         base.Dispose();

         _sut?.Dispose();
      }
   }
}
