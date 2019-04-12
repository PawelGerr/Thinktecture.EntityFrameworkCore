using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.ValueConversion.RowVersionValueConverterTests
{
   public class ConvertToProvider
   {
      private RowVersionValueConverter SUT = new RowVersionValueConverter();

      [Fact]
      public void Should_convert_0_array_with_zeros()
      {
         SUT.ConvertToProvider(0UL).Should().BeOfType<byte[]>()
            .And.Subject.As<byte[]>().Should().BeEquivalentTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
      }

      [Fact]
      public void Should_convert_42_to_byte_array()
      {
         var bytes = new byte[] { 42, 0, 0, 0, 0, 0, 0, 0 };

         if (BitConverter.IsLittleEndian)
            bytes = bytes.Reverse().ToArray();

         SUT.ConvertToProvider(42UL).As<byte[]>().Should().BeEquivalentTo(bytes);
      }

      [Fact]
      public void Should_throw_if_parameter_not_ulong_and_cannot_be_converted()
      {
         SUT.Invoking(sut => sut.ConvertToProvider(new int[0])).Should().Throw<InvalidCastException>();
      }

      [Fact]
      public void Should_convert_string_42_to_byte_array()
      {
         var bytes = new byte[] { 42, 0, 0, 0, 0, 0, 0, 0 };

         if (BitConverter.IsLittleEndian)
            bytes = bytes.Reverse().ToArray();

         SUT.ConvertToProvider("42").As<byte[]>().Should().BeEquivalentTo(bytes);
      }

      // actually, it is the behavior of the base class
      [Fact]
      public void Should_return_null_if_parameter_is_null()
      {
         SUT.ConvertToProvider(null).Should().BeNull();
      }
   }
}