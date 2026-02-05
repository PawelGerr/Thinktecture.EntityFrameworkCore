using Microsoft.Data.Sqlite;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class SqliteBulkOperationContextFactoryForValues : ISqliteBulkOperationContextFactory
{
   public static readonly ISqliteBulkOperationContextFactory Instance = new SqliteBulkOperationContextFactoryForValues();

   public ISqliteBulkOperationContext CreateForBulkInsert(DbContext ctx, SqliteBulkInsertOptions options, IReadOnlyList<PropertyWithNavigations> properties)
   {
      return new BulkInsertValueContext((SqliteConnection)ctx.Database.GetDbConnection(), options, properties);
   }
}
