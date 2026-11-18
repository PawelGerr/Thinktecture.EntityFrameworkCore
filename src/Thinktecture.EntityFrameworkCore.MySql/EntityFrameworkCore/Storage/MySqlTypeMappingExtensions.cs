using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Storage;

internal static class MySqlTypeMappingExtensions
{
   /// <summary>
   /// The provider maps an explicit column type like <c>datetime(6)</c> to a mapping with the size 6 and without a precision,
   /// and the SQL literal of such a mapping has no fractional seconds. For date and time types the size is the fractional precision,
   /// so this method returns a mapping with that precision. For other types (e.g. strings) the size is a length, and the mapping is returned unchanged.
   /// </summary>
   public static RelationalTypeMapping WithFractionalSecondsPrecision(this RelationalTypeMapping mapping)
   {
      if (mapping is not { Precision: null, Size: { } size })
         return mapping;

      // With a value converter, the literal is generated for the provider type.
      var type = mapping.Converter?.ProviderClrType ?? mapping.ClrType;
      type = Nullable.GetUnderlyingType(type) ?? type;

      return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(TimeOnly)
                ? mapping.WithPrecisionAndScale(size, null)
                : mapping;
   }
}
