using System.Text;
using System.Text.Json;

namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <summary>
/// Represents a collection parameter.
/// </summary>
public class JsonCollectionParameter<T, TConverted> : JsonCollectionParameter
{
   private readonly IEnumerable<T> _values;
   private readonly Func<object?, object?> _convertValue;
   private readonly JsonSerializerOptions _jsonSerializerOptions;

   /// <summary>
   /// Initializes a new instance of <see cref="JsonCollectionParameter{T}"/>.
   /// </summary>
   /// <param name="values">Values to serialize.</param>
   /// <param name="convertValue">EF value converter.</param>
   /// <param name="jsonSerializerOptions">JSON serialization settings.</param>
   public JsonCollectionParameter(
      IEnumerable<T> values,
      JsonSerializerOptions jsonSerializerOptions,
      Func<object?, object?> convertValue)
   {
      _values = values ?? throw new ArgumentNullException(nameof(values));
      _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
      _convertValue = convertValue ?? throw new ArgumentNullException(nameof(convertValue));
   }

   /// <inheritdoc />
   public override string ToString(IFormatProvider? provider)
   {
      var convert = _convertValue;
      var values = _values.Select(i => (TConverted)convert(i)!);

      return JsonSerializer.Serialize(values, typeof(IEnumerable<TConverted>), _jsonSerializerOptions);
   }
}

/// <summary>
/// Represents a collection parameter.
/// </summary>
public class JsonCollectionParameter<T> : JsonCollectionParameter
{
   private readonly IEnumerable<T> _values;
   private readonly JsonSerializerOptions _jsonSerializerOptions;

   /// <summary>
   /// Initializes a new instance of <see cref="JsonCollectionParameter{T}"/>.
   /// </summary>
   /// <param name="values">Values to serialize.</param>
   /// <param name="jsonSerializerOptions">JSON serialization settings.</param>
   public JsonCollectionParameter(
      IEnumerable<T> values,
      JsonSerializerOptions jsonSerializerOptions)
   {
      _values = values ?? throw new ArgumentNullException(nameof(values));
      _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
   }

   /// <inheritdoc />
   public override string ToString(IFormatProvider? provider)
   {
      return JsonSerializer.Serialize(_values, typeof(IEnumerable<T>), _jsonSerializerOptions);
   }
}

/// <summary>
/// Represents a collection parameter.
/// </summary>
public abstract class JsonCollectionParameter : IConvertible
{
   /// <inheritdoc />
   public abstract string ToString(IFormatProvider? provider);

#pragma warning disable CS1591
   // ReSharper disable ArrangeMethodOrOperatorBody
   public TypeCode GetTypeCode() => throw new NotSupportedException();
   public bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();
   public byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();
   public char ToChar(IFormatProvider? provider) => throw new NotSupportedException();
   public DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();
   public decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();
   public double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();
   public short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();
   public int ToInt32(IFormatProvider? provider) => throw new NotSupportedException();
   public long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();
   public sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();
   public float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();
   public object ToType(Type conversionType, IFormatProvider? provider) => throw new NotSupportedException();
   public ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();
   public uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();
   public ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();
   // ReSharper restore ArrangeMethodOrOperatorBody
#pragma warning restore CS1591
}
