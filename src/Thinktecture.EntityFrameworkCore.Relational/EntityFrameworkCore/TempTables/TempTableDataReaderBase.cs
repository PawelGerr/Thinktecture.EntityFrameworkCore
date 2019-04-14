using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Data reader to be used for bulk inserts.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   public abstract class TempTableDataReaderBase<T> : ITempTableDataReaderBase
      where T : class
   {
      private readonly IEnumerator<T> _enumerator;

      /// <summary>
      /// Gets the element in the collection at the current position of the enumerator.
      /// </summary>
      [NotNull]
      protected T Current => _enumerator.Current ?? throw new InvalidOperationException("The entities to insert cannot be null.");

      /// <inheritdoc />
      public abstract int FieldCount { get; }

      /// <summary>
      /// Initializes <see cref="TempTableDataReaderBase{T}"/>
      /// </summary>
      /// <param name="entities"></param>
      protected TempTableDataReaderBase([NotNull] IEnumerable<T> entities)
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));

         _enumerator = entities.GetEnumerator();
      }

      /// <summary>
      /// Gets the index of the provided <paramref name="propertyInfo"/> used by <see cref="GetValue"/>.
      /// </summary>
      /// <param name="propertyInfo">Property info to get index for.</param>
      /// <returns>Index of the property.</returns>
      public abstract int GetPropertyIndex(PropertyInfo propertyInfo);

      /// <inheritdoc />
      public abstract object GetValue(int i);

      /// <inheritdoc />
      public bool Read()
      {
         return _enumerator.MoveNext();
      }

      /// <inheritdoc />
      public bool IsDBNull(int i)
      {
         return false;
      }

      /// <inheritdoc />
      public void Dispose()
      {
         _enumerator.Dispose();
      }

      // The following methods are not needed for bulk insert.
      // ReSharper disable ArrangeMethodOrOperatorBody
#pragma warning disable 1591
      public int Depth => throw new NotImplementedException();
      public int RecordsAffected => throw new NotImplementedException();
      public bool IsClosed => throw new NotImplementedException();
      public void Close() => throw new NotImplementedException();
      public string GetName(int i) => throw new NotImplementedException();
      public string GetDataTypeName(int i) => throw new NotImplementedException();
      public Type GetFieldType(int i) => throw new NotImplementedException();
      public int GetValues(object[] values) => throw new NotImplementedException();
      public int GetOrdinal(string name) => throw new NotImplementedException();
      public bool GetBoolean(int i) => throw new NotImplementedException();
      public byte GetByte(int i) => throw new NotImplementedException();
      public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
      public char GetChar(int i) => throw new NotImplementedException();
      public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
      public Guid GetGuid(int i) => throw new NotImplementedException();
      public short GetInt16(int i) => throw new NotImplementedException();
      public int GetInt32(int i) => throw new NotImplementedException();
      public long GetInt64(int i) => throw new NotImplementedException();
      public float GetFloat(int i) => throw new NotImplementedException();
      public double GetDouble(int i) => throw new NotImplementedException();
      public string GetString(int i) => throw new NotImplementedException();
      public decimal GetDecimal(int i) => throw new NotImplementedException();
      public DateTime GetDateTime(int i) => throw new NotImplementedException();
      public IDataReader GetData(int i) => throw new NotImplementedException();
      public DataTable GetSchemaTable() => throw new NotImplementedException();
      public bool NextResult() => throw new NotImplementedException();

      object IDataRecord.this[int i] => throw new NotImplementedException();
      object IDataRecord.this[string name] => throw new NotImplementedException();
#pragma warning restore 1591
      // ReSharper restore ArrangeMethodOrOperatorBody
   }
}
