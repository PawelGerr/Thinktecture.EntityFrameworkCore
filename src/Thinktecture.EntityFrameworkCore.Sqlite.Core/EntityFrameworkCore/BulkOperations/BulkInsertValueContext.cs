using Microsoft.Data.Sqlite;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkInsertValueContext : ISqliteBulkOperationContext
{
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqliteConnection Connection { get; }
   public SqliteBulkInsertOptions Options { get; }

   public SqliteAutoIncrementBehavior AutoIncrementBehavior => Options.AutoIncrementBehavior;
   public bool HasExternalProperties => false;

   public BulkInsertValueContext(
      SqliteConnection connection,
      SqliteBulkInsertOptions options,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      Properties = properties;
      Connection = connection;
      Options = options;
   }

   public IEntityDataReader<T> CreateReader<T>(IEnumerable<T> values)
   {
      return new ValueDataReader<T>(values, Properties);
   }

   public SqliteCommandBuilder CreateCommandBuilder()
   {
      return SqliteCommandBuilder.Insert(Properties);
   }

   public IReadOnlyList<ISqliteOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      throw new InvalidOperationException("BulkInsertValueContext has no children.");
   }
}
