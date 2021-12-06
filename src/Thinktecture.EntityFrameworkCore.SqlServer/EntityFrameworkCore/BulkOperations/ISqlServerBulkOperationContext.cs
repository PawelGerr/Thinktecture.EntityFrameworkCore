using Microsoft.Data.SqlClient;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface ISqlServerBulkOperationContext : IBulkOperationContext
{
   SqlConnection Connection { get; }
   SqlTransaction? Transaction { get; }
   ISqlServerBulkInsertOptions Options { get; }

   IReadOnlyList<ISqlServerOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities);

   IReadOnlyList<IOwnedTypeBulkOperationContext> IBulkOperationContext.GetContextsForExternalOwnedTypes(IReadOnlyList<object> entities)
   {
      return GetChildren(entities);
   }
}
