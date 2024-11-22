using Microsoft.Data.Sqlite;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal interface ISqliteBulkOperationContext : IBulkOperationContext
{
   SqliteAutoIncrementBehavior AutoIncrementBehavior { get; }
   SqliteConnection Connection { get; }
   SqliteCommandBuilder CreateCommandBuilder();

   IReadOnlyList<ISqliteOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities);
}
