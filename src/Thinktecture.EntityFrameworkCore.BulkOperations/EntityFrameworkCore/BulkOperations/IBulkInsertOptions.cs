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
