using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class SqlServerBulkOperationContextFactoryForValues : ISqlServerBulkOperationContextFactory
{
   public static readonly ISqlServerBulkOperationContextFactory Instance = new SqlServerBulkOperationContextFactoryForValues();

   public ISqlServerBulkOperationContext CreateForBulkInsert(DbContext ctx, SqlServerBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new BulkInsertValueContext(properties,
                                        (SqlConnection)ctx.Database.GetDbConnection(),
                                        (SqlTransaction?)ctx.Database.CurrentTransaction?.GetDbTransaction(),
                                        options);
   }
}
