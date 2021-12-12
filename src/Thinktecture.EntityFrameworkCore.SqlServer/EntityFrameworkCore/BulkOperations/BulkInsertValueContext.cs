using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkInsertValueContext : ISqlServerBulkOperationContext
{
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqlConnection Connection { get; }
   public SqlTransaction? Transaction { get; }
   public SqlServerBulkInsertOptions Options { get; }

   public bool HasExternalProperties => false;

   public BulkInsertValueContext(
      IReadOnlyList<PropertyWithNavigations> properties,
      SqlConnection connection,
      SqlTransaction? transaction,
      SqlServerBulkInsertOptions options)
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

   public IReadOnlyList<ISqlServerOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      throw new InvalidOperationException("BulkInsertValueContext has no children.");
   }
}
