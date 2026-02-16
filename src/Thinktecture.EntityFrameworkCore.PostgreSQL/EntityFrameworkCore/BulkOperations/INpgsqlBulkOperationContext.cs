using Npgsql;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface INpgsqlBulkOperationContext : IBulkOperationContext
{
   NpgsqlConnection Connection { get; }
   NpgsqlTransaction? Transaction { get; }
   NpgsqlBulkInsertOptions Options { get; }

   IReadOnlyList<INpgsqlOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities);
}
