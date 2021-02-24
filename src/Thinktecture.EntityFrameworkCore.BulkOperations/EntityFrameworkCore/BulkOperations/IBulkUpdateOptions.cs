namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options.
   /// </summary>
   public interface IBulkUpdateOptions
   {
      /// <summary>
      /// Properties to update.
      /// If the <see cref="PropertiesToUpdate"/> is <c>null</c> then all properties of the entity are going to be inserted.
      /// </summary>
      public IEntityPropertiesProvider? PropertiesToUpdate { get; }

      /// <summary>
      /// Properties to perform the JOIN/match on.
      /// The primary key of the entity is used by default.
      /// </summary>
      public IEntityPropertiesProvider? KeyProperties { get; }
   }
}
