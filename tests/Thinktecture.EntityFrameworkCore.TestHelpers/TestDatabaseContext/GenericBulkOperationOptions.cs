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
   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'. Note: 'ForRootEntity' emits no schema when 'schema' is null, while 'TableName' alone keeps the schema of the EF Core model.")]
   public string? TableName { get; init; }

   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'.")]
   public string? Schema { get; init; }

   public IBulkOperationTableNameProvider TableNameProvider { get; init; } = IBulkOperationTableNameProvider.Default;
}
