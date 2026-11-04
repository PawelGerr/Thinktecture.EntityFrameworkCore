using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Options that are not provider-specific, so the executors have to copy them into their own options type.
/// </summary>
public sealed class GenericBulkOperationOptions : IBulkInsertOptions, IBulkUpdateOptions, IBulkInsertOrUpdateOptions
{
   public IEntityPropertiesProvider? PropertiesToInsert { get; init; }
   public IEntityPropertiesProvider? PropertiesToUpdate { get; init; }
   public IEntityPropertiesProvider? KeyProperties { get; init; }
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well.")]
   public string? TableName { get; init; }

   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(schema)' when only 'Schema' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'TableName' is set as well.")]
   public string? Schema { get; init; }

   public IBulkOperationTableNameProvider TableNameProvider { get; init; } = IBulkOperationTableNameProvider.Default;
}
