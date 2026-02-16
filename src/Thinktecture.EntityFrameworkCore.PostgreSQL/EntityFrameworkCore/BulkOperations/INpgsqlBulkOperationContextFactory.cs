using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface INpgsqlBulkOperationContextFactory
{
   INpgsqlBulkOperationContext CreateForBulkInsert(DbContext ctx, NpgsqlBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties);
}
