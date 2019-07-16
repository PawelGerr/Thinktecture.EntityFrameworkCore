using System;
using System.Collections.Generic;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests
{
   public class GetPropertyIndex : TestBase
   {
      private readonly List<IProperty> _propertiesToRead = new List<IProperty>();
      private readonly IProperty _column1;
      private readonly IProperty _column2;

      private EntityDataReader<CustomTempTable> _sut;

      public GetPropertyIndex()
      {
         DbContextWithSchema.ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         _column1 = DbContextWithSchema.GetEntityType<CustomTempTable>().GetProperty(nameof(CustomTempTable.Column1));
         _column2 = DbContextWithSchema.GetEntityType<CustomTempTable>().GetProperty(nameof(CustomTempTable.Column2));
      }

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReader<CustomTempTable> SUT => _sut ?? (_sut = new EntityDataReader<CustomTempTable>(Array.Empty<CustomTempTable>(), _propertiesToRead));

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

      public override void Dispose()
      {
         base.Dispose();

         _sut?.Dispose();
      }
   }
}
