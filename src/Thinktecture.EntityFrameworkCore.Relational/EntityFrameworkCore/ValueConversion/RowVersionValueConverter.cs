using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Thinktecture.EntityFrameworkCore.ValueConversion
{
   /// <summary>
   /// Value converter for row version.
   /// </summary>
   public class RowVersionValueConverter : ValueConverter<long, byte[]>
   {
      /// <summary>
      /// An instance of <see cref="RowVersionValueConverter"/>.
      /// </summary>
      public static readonly RowVersionValueConverter Instance = new RowVersionValueConverter();

      /// <summary>
      /// Initializes new instance of <see cref="RowVersionValueConverter"/>.
      /// </summary>
      public RowVersionValueConverter()
         : base(GetToBytesExpression(), GetToNumberExpression())
      {
      }

      [NotNull]
      private static Expression<Func<byte[], long>> GetToNumberExpression()
      {
         if (!BitConverter.IsLittleEndian)
            return bytes => ConvertBytes(bytes);

         return bytes => ConvertBytes(ReverseLong(bytes));
      }

      private static long ConvertBytes([NotNull] byte[] bytes)
      {
         return bytes.Length == 0 ? 0 : BitConverter.ToInt64(bytes, 0);
      }

      [NotNull]
      private static Expression<Func<long, byte[]>> GetToBytesExpression()
      {
         if (!BitConverter.IsLittleEndian)
            return rowVersion => BitConverter.GetBytes(rowVersion);

         return rowVersion => ReverseLong(BitConverter.GetBytes(rowVersion));
      }

      [NotNull]
      private static byte[] ReverseLong([NotNull] byte[] bytes)
      {
         if (bytes.Length == 0)
            return bytes;

         return new[]
                {
                   bytes[7],
                   bytes[6],
                   bytes[5],
                   bytes[4],
                   bytes[3],
                   bytes[2],
                   bytes[1],
                   bytes[0]
                };
      }
   }
}
