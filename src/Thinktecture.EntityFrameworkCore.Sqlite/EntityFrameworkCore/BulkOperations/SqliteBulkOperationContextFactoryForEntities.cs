using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class SqliteBulkOperationContextFactoryForEntities : ISqliteBulkOperationContextFactory
{
   public static readonly ISqliteBulkOperationContextFactory Instance = new SqliteBulkOperationContextFactoryForEntities();

   public ISqliteBulkOperationContext CreateForBulkInsert(DbContext ctx, SqliteBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new BulkInsertContext(ctx,
                                   ctx.GetService<IEntityDataReaderFactory>(),
                                   (SqliteConnection)ctx.Database.GetDbConnection(),
                                   options,
                                   properties);
   }
}
