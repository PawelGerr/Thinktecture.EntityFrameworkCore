namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options.
   /// </summary>
   public interface IBulkUpdateOptions
   {
      /// <summary>
      /// Options for creation of the temp table and for bulk insert of data used for later update.
      /// </summary>
      ITempTableBulkInsertOptions TempTableOptions { get; }

      /// <summary>
      /// Properties to update.
      /// If the <see cref="MembersToUpdate"/> is <c>null</c> then all properties of the entity are going to be inserted.
      /// </summary>
      IEntityMembersProvider? MembersToUpdate { get; set; }

      /// <summary>
      /// Properties to perform the JOIN/match on.
      /// The primary key of the entity is used by default.
      /// </summary>
      IEntityMembersProvider? KeyProperties { get; set; }
   }
}
