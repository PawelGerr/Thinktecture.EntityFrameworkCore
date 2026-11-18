using System.Data;
using System.Runtime.ExceptionServices;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Streams the rows of an <see cref="IEntityDataReader" /> to <see cref="MySqlConnector.MySqlBulkCopy" />.
/// MySqlBulkCopy reads a reader that is not a <see cref="System.Data.Common.DbDataReader" /> through <see cref="FieldCount" />, <see cref="Read" /> and <see cref="GetValues" />,
/// and the entity data readers do not implement <see cref="GetValues" />.
/// The reader stores an exception of the source in <see cref="SourceException" /> instead of letting it escape to MySqlBulkCopy.
/// </summary>
internal sealed class MySqlBulkCopyDataReader : IDataReader
{
   private readonly IEntityDataReader _reader;
   private readonly object[] _values;

   /// <summary>
   /// The exception of the source (enumeration, property getter or value converter) that ended the copy, or <c>null</c>.
   /// </summary>
   public ExceptionDispatchInfo? SourceException { get; private set; }

   public MySqlBulkCopyDataReader(IEntityDataReader reader)
   {
      _reader = reader ?? throw new ArgumentNullException(nameof(reader));
      _values = new object[reader.FieldCount];
   }

   public int FieldCount => _values.Length;

   // Reads the next row with all its values, so that GetValues cannot throw.
   // MySqlConnector marks the session as failed for an exception that escapes during LOAD DATA LOCAL, which closes the connection
   // and rolls back the transaction of the caller. Therefore the exception is stored and the copy ends as if the source had no more rows.
   public bool Read()
   {
      if (SourceException is not null)
         return false;

      try
      {
         if (!_reader.Read())
            return false;

         for (var i = 0; i < _values.Length; i++)
         {
            _values[i] = _reader.GetValue(i) ?? DBNull.Value;
         }

         return true;
      }
      catch (Exception exception)
      {
         SourceException = ExceptionDispatchInfo.Capture(exception);
         return false;
      }
   }

   public int GetValues(object[] values)
   {
      ArgumentNullException.ThrowIfNull(values);

      var count = Math.Min(values.Length, _values.Length);
      Array.Copy(_values, values, count);

      return count;
   }

   // The caller owns the inner reader.
   public void Dispose()
   {
   }

   public void Close()
   {
   }

   public object this[int i] => throw new NotSupportedException();
   public object this[string name] => throw new NotSupportedException();
   public int Depth => throw new NotSupportedException();
   public bool IsClosed => throw new NotSupportedException();
   public int RecordsAffected => throw new NotSupportedException();
   public bool GetBoolean(int i) => throw new NotSupportedException();
   public byte GetByte(int i) => throw new NotSupportedException();
   public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
   public char GetChar(int i) => throw new NotSupportedException();
   public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
   public IDataReader GetData(int i) => throw new NotSupportedException();
   public string GetDataTypeName(int i) => throw new NotSupportedException();
   public DateTime GetDateTime(int i) => throw new NotSupportedException();
   public decimal GetDecimal(int i) => throw new NotSupportedException();
   public double GetDouble(int i) => throw new NotSupportedException();
   public Type GetFieldType(int i) => throw new NotSupportedException();
   public float GetFloat(int i) => throw new NotSupportedException();
   public Guid GetGuid(int i) => throw new NotSupportedException();
   public short GetInt16(int i) => throw new NotSupportedException();
   public int GetInt32(int i) => throw new NotSupportedException();
   public long GetInt64(int i) => throw new NotSupportedException();
   public string GetName(int i) => throw new NotSupportedException();
   public int GetOrdinal(string name) => throw new NotSupportedException();
   public DataTable GetSchemaTable() => throw new NotSupportedException();
   public string GetString(int i) => throw new NotSupportedException();
   public object GetValue(int i) => throw new NotSupportedException();
   public bool IsDBNull(int i) => throw new NotSupportedException();
   public bool NextResult() => throw new NotSupportedException();
}
