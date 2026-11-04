namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for the query-based bulk insert and bulk update operations.
/// </summary>
public interface IBulkOperationFromQueryOptions
{
   /// <summary>
   /// Overrides the target table name. The schema of the EF Core model is kept.
   /// Use <see cref="TableNameProvider"/> instead.
   /// </summary>
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well.")]
   string? TableName { get; }

   /// <summary>
   /// Overrides the target schema. The table name of the EF Core model is kept.
   /// Use <see cref="TableNameProvider"/> instead.
   /// </summary>
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(schema)' when only 'Schema' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'TableName' is set as well.")]
   string? Schema { get; }

   /// <summary>
   /// Provides the target table name.
   /// The provider is called for the target entity type only, because a query-based operation writes one table.
   /// Defaults to <see cref="IBulkOperationTableNameProvider.Default"/>, i.e. the table of the EF Core model.
   /// Setting the obsolete <see cref="TableName"/> or <see cref="Schema"/> on the options of this library replaces this provider with a provider for the root entity.
   /// Setting this property clears <see cref="TableName"/> and <see cref="Schema"/>.
   /// The last assignment wins.
   /// Implementations that do not override this member get a provider built from <see cref="TableName"/> and <see cref="Schema"/>, or <see cref="IBulkOperationTableNameProvider.Default"/> when both are <c>null</c>.
   /// </summary>
#pragma warning disable CS0618 // Implementations that do not override 'TableNameProvider' get a provider built from the obsolete 'TableName' and 'Schema'.
   IBulkOperationTableNameProvider TableNameProvider => RootEntityTableNameProviderBase.FromObsoleteOptions(TableName, Schema) ?? IBulkOperationTableNameProvider.Default;
#pragma warning restore CS0618
}
