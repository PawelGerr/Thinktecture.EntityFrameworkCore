using Npgsql;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class NpgsqlBulkInsertValueContext : INpgsqlBulkOperationContext
{
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public NpgsqlConnection Connection { get; }
   public NpgsqlTransaction? Transaction { get; }
   public NpgsqlBulkInsertOptions Options { get; }

   public bool HasExternalProperties => false;

   public NpgsqlBulkInsertValueContext(
      IReadOnlyList<PropertyWithNavigations> properties,
      NpgsqlConnection connection,
      NpgsqlTransaction? transaction,
      NpgsqlBulkInsertOptions options)
   {
      Properties = properties;
      Connection = connection;
      Transaction = transaction;
      Options = options;
   }

   public IEntityDataReader<T> CreateReader<T>(IEnumerable<T> values)
   {
      return new ValueDataReader<T>(values, Properties);
   }

   public IReadOnlyList<INpgsqlOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      throw new InvalidOperationException("NpgsqlBulkInsertValueContext has no children.");
   }
}
