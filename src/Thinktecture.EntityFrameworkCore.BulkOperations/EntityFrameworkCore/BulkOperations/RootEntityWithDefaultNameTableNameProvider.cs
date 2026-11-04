using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the table name of <see cref="IBulkOperationTableNameProvider.ForRootEntityWithDefaultName"/>.
/// The table name comes from <see cref="IBulkOperationTableNameProvider.Default"/>, so a temp table entity throws because it has no regular table.
/// A <c>null</c> <paramref name="schema"/> means that no schema is emitted.
/// </summary>
/// <param name="schema">Schema of the target table.</param>
internal sealed class RootEntityWithDefaultNameTableNameProvider(string? schema) : RootEntityTableNameProviderBase
{
   protected override BulkOperationTableName GetRootTableName(IEntityType entityType)
   {
      return new BulkOperationTableName(IBulkOperationTableNameProvider.Default.GetTableName(entityType).Name, schema);
   }
}
