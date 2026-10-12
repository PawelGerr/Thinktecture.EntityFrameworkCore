namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options.
/// </summary>
public interface IBulkInsertOptions
{
   /// <summary>
   /// Properties to insert.
   /// If the <see cref="PropertiesToInsert"/> is null then all properties of the entity are going to be inserted.
   /// </summary>
   IEntityPropertiesProvider? PropertiesToInsert { get; }

   /// <summary>
   /// Overrides the table name of the root entity. The schema of the EF Core model is kept.
   /// Owned types that are mapped to separate tables are not supported, and the operation throws when it reaches such an owned type.
   /// Rows of the root table may already be written at that point. Atomicity is the responsibility of the caller's transaction.
   /// Use <see cref="TableNameProvider"/> instead.
   /// </summary>
   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'. Note: 'ForRootEntity' emits no schema when 'schema' is null, while 'TableName' alone keeps the schema of the EF Core model.")]
   string? TableName { get; }

   /// <summary>
   /// Overrides the schema of the root entity. The table name of the EF Core model is kept.
   /// Owned types that are mapped to separate tables are not supported, and the operation throws when it reaches such an owned type.
   /// Rows of the root table may already be written at that point. Atomicity is the responsibility of the caller's transaction.
   /// Use <see cref="TableNameProvider"/> instead.
   /// </summary>
   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'.")]
   string? Schema { get; }

   /// <summary>
   /// Provides the target table names of the entity and of its owned types that are mapped to separate tables.
   /// Defaults to <see cref="IBulkOperationTableNameProvider.Default"/>, i.e. the tables of the EF Core model.
   /// Setting the obsolete <see cref="TableName"/> or <see cref="Schema"/> on the options of this library replaces this provider with a provider for the root entity.
   /// Setting this property clears <see cref="TableName"/> and <see cref="Schema"/>.
   /// The last assignment wins.
   /// Implementations that do not override this member get a provider built from <see cref="TableName"/> and <see cref="Schema"/>, or <see cref="IBulkOperationTableNameProvider.Default"/> when both are <c>null</c>.
   /// </summary>
#pragma warning disable CS0618 // Implementations that do not override 'TableNameProvider' get a provider built from the obsolete 'TableName' and 'Schema'.
   IBulkOperationTableNameProvider TableNameProvider => ObsoleteRootEntityTableNameProvider.FromOptions(TableName, Schema) ?? IBulkOperationTableNameProvider.Default;
#pragma warning restore CS0618
}
