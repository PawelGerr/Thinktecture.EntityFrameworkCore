using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class NpgsqlBulkOperationContextFactoryForValues : INpgsqlBulkOperationContextFactory
{
   public static readonly INpgsqlBulkOperationContextFactory Instance = new NpgsqlBulkOperationContextFactoryForValues();

   public INpgsqlBulkOperationContext CreateForBulkInsert(DbContext ctx, NpgsqlBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new NpgsqlBulkInsertValueContext(properties,
                                              (NpgsqlConnection)ctx.Database.GetDbConnection(),
                                              (NpgsqlTransaction?)ctx.Database.CurrentTransaction?.GetDbTransaction(),
                                              options);
   }
}
