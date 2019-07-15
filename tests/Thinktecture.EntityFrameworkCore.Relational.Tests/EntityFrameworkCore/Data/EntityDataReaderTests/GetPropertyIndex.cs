using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using Thinktecture.TestDatabaseContext;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests
{
   public class GetPropertyIndex : IDisposable
   {
      public delegate object GetValue<in T>(T entity, int index);

      private readonly List<PropertyInfo> _propertiesToRead = new List<PropertyInfo>();
      private readonly List<PropertyInfo> _getValueProperties = new List<PropertyInfo>();
      private readonly Mock<GetValue<CustomTempTable>> _getValueMock = new Mock<GetValue<CustomTempTable>>();
      private readonly PropertyInfo _column1 = typeof(CustomTempTable).GetProperty(nameof(CustomTempTable.Column1));
      private readonly PropertyInfo _column2 = typeof(CustomTempTable).GetProperty(nameof(CustomTempTable.Column2));

      private EntityDataReader<CustomTempTable> _sut;

      [NotNull]
      // ReSharper disable once InconsistentNaming
      private EntityDataReader<CustomTempTable> SUT => _sut ?? (_sut = new EntityDataReader<CustomTempTable>(Array.Empty<CustomTempTable>(), _propertiesToRead, _getValueProperties, (e, i) => _getValueMock.Object(e, i)));

      [Fact]
      public void Should_throw_if_property_is_in_propertiesToRead()
      {
         _propertiesToRead.Add(_column1);
         _getValueProperties.Add(_column1);
         _getValueProperties.Add(_column2);

         SUT.Invoking(sut => sut.GetPropertyIndex(_column2))
            .Should().Throw<ArgumentException>();
      }

      [Fact]
      public void Should_return_index_of_the_property()
      {
         _propertiesToRead.Add(_column2);
         _getValueProperties.Add(_column1);
         _getValueProperties.Add(_column2);

         SUT.GetPropertyIndex(_column2).Should().Be(1);
      }

      public void Dispose()
      {
         _sut?.Dispose();
      }
   }
}
