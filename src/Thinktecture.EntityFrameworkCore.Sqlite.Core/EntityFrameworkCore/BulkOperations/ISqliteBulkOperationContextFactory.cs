using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface ISqliteBulkOperationContextFactory
{
   ISqliteBulkOperationContext CreateForBulkInsert(DbContext ctx, SqliteBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties);
}
