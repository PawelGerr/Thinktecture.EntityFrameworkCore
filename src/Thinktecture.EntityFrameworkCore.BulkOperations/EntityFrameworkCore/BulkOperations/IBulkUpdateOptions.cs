namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options.
   /// </summary>
   public interface IBulkUpdateOptions
   {
      /// <summary>
      /// Properties to update.
      /// If the <see cref="MembersToUpdate"/> is <c>null</c> then all properties of the entity are going to be inserted.
      /// </summary>
      public IEntityMembersProvider? MembersToUpdate { get; set; }

      /// <summary>
      /// Properties to perform the JOIN/match on.
      /// The primary key of the entity is used by default.
      /// </summary>
      public IEntityMembersProvider? KeyProperties { get; set; }
   }
}
