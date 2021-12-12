using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface ISqlServerBulkOperationContextFactory
{
   ISqlServerBulkOperationContext CreateForBulkInsert(DbContext ctx, SqlServerBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties);
}
