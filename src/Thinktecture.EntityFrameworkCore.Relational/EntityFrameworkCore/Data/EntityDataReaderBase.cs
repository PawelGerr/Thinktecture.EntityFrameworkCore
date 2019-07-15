using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Data reader to be used for bulk inserts.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   public abstract class EntityDataReaderBase<T> : IEntityDataReader
      where T : class
   {
      private readonly IEnumerator<T> _enumerator;

      /// <summary>
      /// Gets the element in the collection at the current position of the enumerator.
      /// </summary>
      [NotNull]
      protected T Current => _enumerator.Current ?? throw new InvalidOperationException("The entities to insert cannot be null.");

      /// <inheritdoc />
      public IReadOnlyList<PropertyInfo> Properties { get; }

      /// <inheritdoc />
      public int FieldCount => Properties.Count;

      /// <summary>
      /// Initializes <see cref="EntityDataReaderBase{T}"/>
      /// </summary>
      /// <param name="entities">Entities to read.</param>
      /// <param name="properties">Properties to read.</param>
      protected EntityDataReaderBase([NotNull] IEnumerable<T> entities, [NotNull] IReadOnlyList<PropertyInfo> properties)
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));
         if (properties.Count == 0)
            throw new ArgumentException("The properties collection cannot be null.", nameof(properties));

         Properties = properties;
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
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      /// <summary>
      /// Disposes of inner resources.
      /// </summary>
      /// <param name="disposing">Indication whether this method is being called by the method <see cref="EntityDataReaderBase{T}.Dispose()"/>.</param>
      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
            _enumerator.Dispose();
      }

      // The following methods are not needed for bulk insert.
      // ReSharper disable ArrangeMethodOrOperatorBody
#pragma warning disable 1591
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
      long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
      char IDataRecord.GetChar(int i) => throw new NotSupportedException();
      long IDataRecord.GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
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
#pragma warning restore 1591
      // ReSharper restore ArrangeMethodOrOperatorBody
   }
}
