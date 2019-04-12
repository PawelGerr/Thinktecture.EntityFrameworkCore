using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.ValueConversion.RowVersionValueConverterTests
{
   public class ConvertFromProvider
   {
      private RowVersionValueConverter SUT = new RowVersionValueConverter();

      [Fact]
      public void Should_convert_empty_byte_array_to_0()
      {
         SUT.ConvertFromProvider(new byte[0]).Should().Be(0UL);
      }

      [Fact]
      public void Should_throw_if_array_length_not_0_nor_8()
      {
         SUT.Invoking(sut => sut.ConvertFromProvider(new byte[] { 1 })).Should().Throw<IndexOutOfRangeException>();
      }

      [Fact]
      public void Should_throw_if_parameter_not_byte_array()
      {
         SUT.Invoking(sut => sut.ConvertFromProvider(new int[0])).Should().Throw<InvalidCastException>();
      }

      // actually, it is the behavior of the base class
      [Fact]
      public void Should_return_null_if_parameter_is_null()
      {
         SUT.ConvertFromProvider(null).Should().BeNull();
      }

      [Fact]
      public void Should_convert_byte_array_to_number()
      {
         var bytes = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };

         if (BitConverter.IsLittleEndian)
            bytes = bytes.Reverse().ToArray();

         SUT.ConvertFromProvider(bytes).Should().Be(1UL);
      }
   }
}
