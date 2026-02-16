using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class NpgsqlBulkOperationContextFactoryForEntities : INpgsqlBulkOperationContextFactory
{
   public static readonly INpgsqlBulkOperationContextFactory Instance = new NpgsqlBulkOperationContextFactoryForEntities();

   public INpgsqlBulkOperationContext CreateForBulkInsert(DbContext ctx, NpgsqlBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new NpgsqlBulkInsertContext(ctx,
                                         ctx.GetService<IEntityDataReaderFactory>(),
                                         (NpgsqlConnection)ctx.Database.GetDbConnection(),
                                         (NpgsqlTransaction?)ctx.Database.CurrentTransaction?.GetDbTransaction(),
                                         options,
                                         properties);
   }
}
