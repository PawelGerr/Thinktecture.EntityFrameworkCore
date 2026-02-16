using System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Data reader for values of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">Type of the value.</typeparam>
public sealed class ValueDataReader<TValue> : IEntityDataReader<TValue>
{
   private readonly IEnumerator<TValue> _enumerator;

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <inheritdoc />
   public int RowsRead { get; private set; }

   /// <inheritdoc />
   public int FieldCount => 1;

   /// <summary>
   /// Initializes <see cref="ValueDataReader{T}"/>
   /// </summary>
   /// <param name="values">Values to read.</param>
   /// <param name="properties">Properties to read.</param>
   public ValueDataReader(
      IEnumerable<TValue> values,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      ArgumentNullException.ThrowIfNull(values);

      Properties = properties ?? throw new ArgumentNullException(nameof(properties));

      if (properties.Count != 1)
         throw new ArgumentException("The properties collection of a value reader must have exactly 1 property.", nameof(properties));

      _enumerator = values.GetEnumerator();
   }

   /// <inheritdoc />
   public int GetPropertyIndex(PropertyWithNavigations property)
   {
      if (Properties[0].Equals(property))
         return 0;

      throw new ArgumentException($"The property '{property.Property.Name}' of type '{property.Property.ClrType.ShortDisplayName()}' cannot be read by current reader.");
   }

   /// <inheritdoc />
   public IReadOnlyList<TValue> GetReadEntities()
   {
      throw new InvalidOperationException("Value reader has no 'read entities'.");
   }

   /// <inheritdoc />
#pragma warning disable 8766
   public object? GetValue(int i)
#pragma warning restore 8766
   {
      return _enumerator.Current;
   }

   /// <inheritdoc />
   public bool Read()
   {
      if (!_enumerator.MoveNext())
         return false;

      RowsRead++;
      return true;
   }

   /// <inheritdoc />
   public bool IsDBNull(int i)
   {
      return _enumerator.Current is null;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      _enumerator.Dispose();
   }

   // The following methods are not needed for bulk insert.
   // ReSharper disable ArrangeMethodOrOperatorBody
   object IDataRecord.this[int i] => throw new NotSupportedException();
   object IDataRecord.this[string name] => throw new NotSupportedException();
   int IDataReader.Depth => throw new NotSupportedException();
   int IDataReader.RecordsAffected => throw new NotSupportedException();
   bool IDataReader.IsClosed => throw new NotSupportedException();
   void IDataReader.Close() => throw new NotSupportedException();
   string IDataRecord.GetName(int i) => throw new NotSupportedException();
   string IDataRecord.GetDataTypeName(int i) => throw new NotSupportedException();
   Type IDataRecord.GetFieldType(int i) => throw new NotSupportedException();
   int IDataRecord.GetValues(object[] values) => throw new NotSupportedException();
   int IDataRecord.GetOrdinal(string name) => throw new NotSupportedException();
   bool IDataRecord.GetBoolean(int i) => throw new NotSupportedException();
   byte IDataRecord.GetByte(int i) => throw new NotSupportedException();
   long IDataRecord.GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
   char IDataRecord.GetChar(int i) => throw new NotSupportedException();
   long IDataRecord.GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
   Guid IDataRecord.GetGuid(int i) => throw new NotSupportedException();
   short IDataRecord.GetInt16(int i) => throw new NotSupportedException();
   int IDataRecord.GetInt32(int i) => throw new NotSupportedException();
   long IDataRecord.GetInt64(int i) => throw new NotSupportedException();
   float IDataRecord.GetFloat(int i) => throw new NotSupportedException();
   double IDataRecord.GetDouble(int i) => throw new NotSupportedException();
   string IDataRecord.GetString(int i) => throw new NotSupportedException();
   decimal IDataRecord.GetDecimal(int i) => throw new NotSupportedException();
   DateTime IDataRecord.GetDateTime(int i) => throw new NotSupportedException();
   IDataReader IDataRecord.GetData(int i) => throw new NotSupportedException();
   DataTable IDataReader.GetSchemaTable() => throw new NotSupportedException();

   bool IDataReader.NextResult() => throw new NotSupportedException();
   // ReSharper restore ArrangeMethodOrOperatorBody
}
