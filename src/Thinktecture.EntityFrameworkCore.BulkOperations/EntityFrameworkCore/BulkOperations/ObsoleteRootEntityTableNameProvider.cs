using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the table name of the obsolete options 'TableName' and 'Schema'.
/// A <c>null</c> <paramref name="tableName"/> or <paramref name="schema"/> keeps the value of the EF Core model.
/// </summary>
/// <param name="tableName">Name of the target table.</param>
/// <param name="schema">Schema of the target table.</param>
internal sealed class ObsoleteRootEntityTableNameProvider(string? tableName, string? schema) : RootEntityTableNameProviderBase
{
   /// <summary>
   /// Builds the provider of the obsolete options 'TableName' and 'Schema'.
   /// </summary>
   /// <returns>A provider for the root entity type only, or <c>null</c> when both <paramref name="tableName"/> and <paramref name="schema"/> are <c>null</c>.</returns>
   public static IBulkOperationTableNameProvider? FromOptions(string? tableName, string? schema)
   {
      return tableName is null && schema is null ? null : new ObsoleteRootEntityTableNameProvider(tableName, schema);
   }

   protected override BulkOperationTableName GetRootTableName(IEntityType entityType)
   {
      // Obsolete semantics: a null part keeps the value of the EF Core model.
      // The table name comes from 'Default', so that a temp table entity with only 'Schema' set still throws because it has no regular table.
      return new BulkOperationTableName(tableName ?? IBulkOperationTableNameProvider.Default.GetTableName(entityType).Name,
                                        schema ?? entityType.GetSchema());
   }
}
