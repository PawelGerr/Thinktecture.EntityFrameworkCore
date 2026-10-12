using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the table name of <see cref="IBulkOperationTableNameProvider.ForRootEntity"/>.
/// A <c>null</c> <paramref name="schema"/> means that no schema is emitted.
/// </summary>
/// <param name="tableName">Name of the target table.</param>
/// <param name="schema">Schema of the target table.</param>
internal sealed class RootEntityTableNameProvider(string tableName, string? schema) : RootEntityTableNameProviderBase
{
   protected override BulkOperationTableName GetRootTableName(IEntityType entityType)
   {
      return new BulkOperationTableName(tableName, schema);
   }
}
