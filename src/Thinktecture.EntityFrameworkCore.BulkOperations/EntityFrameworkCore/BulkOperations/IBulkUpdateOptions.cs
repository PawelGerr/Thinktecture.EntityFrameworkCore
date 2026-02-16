namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options.
/// </summary>
public interface IBulkUpdateOptions
{
   /// <summary>
   /// Properties to update.
   /// If the <see cref="PropertiesToUpdate"/> is <c>null</c> then all properties of the entity are going to be updated.
   /// </summary>
   IEntityPropertiesProvider? PropertiesToUpdate { get; }

   /// <summary>
   /// Properties to perform the JOIN/match on.
   /// The primary key of the entity is used by default.
   /// </summary>
   IEntityPropertiesProvider? KeyProperties { get; }

   /// <summary>
   /// Overrides the target table name resolved from the entity type.
   /// If <c>null</c>, the table name is resolved from the EF Core model metadata.
   /// </summary>
   string? TableName { get; }

   /// <summary>
   /// Overrides the target schema resolved from the entity type.
   /// If <c>null</c>, the schema is resolved from the EF Core model metadata.
   /// </summary>
   string? Schema { get; }
}
