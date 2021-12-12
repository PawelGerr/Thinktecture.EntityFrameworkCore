using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class SqlServerBulkOperationContextFactoryForEntities : ISqlServerBulkOperationContextFactory
{
   public static readonly ISqlServerBulkOperationContextFactory Instance = new SqlServerBulkOperationContextFactoryForEntities();

   public ISqlServerBulkOperationContext CreateForBulkInsert(DbContext ctx, SqlServerBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new BulkInsertContext(ctx,
                                   ctx.GetService<IEntityDataReaderFactory>(),
                                   (SqlConnection)ctx.Database.GetDbConnection(),
                                   (SqlTransaction?)ctx.Database.CurrentTransaction?.GetDbTransaction(),
                                   options,
                                   properties);
   }
}
