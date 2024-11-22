namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert or update.
/// </summary>
public interface IBulkInsertOrUpdateOptions
{
   /// <summary>
   /// Properties to insert.
   /// If the <see cref="PropertiesToInsert"/> is null then all properties of the entity are going to be inserted.
   /// </summary>
   IEntityPropertiesProvider? PropertiesToInsert { get; }

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
}