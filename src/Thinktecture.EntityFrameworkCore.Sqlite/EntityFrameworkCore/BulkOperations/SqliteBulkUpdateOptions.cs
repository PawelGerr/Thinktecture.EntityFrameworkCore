namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options for SQLite.
   /// </summary>
   public class SqliteBulkUpdateOptions : ISqliteBulkUpdateOptions
   {
      /// <inheritdoc />
      public IEntityMembersProvider? MembersToUpdate { get; set; }

      /// <inheritdoc />
      public IEntityMembersProvider? KeyProperties { get; set; }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteBulkUpdateOptions"/>.
      /// </summary>
      /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
      public SqliteBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
      {
         if (optionsToInitializeFrom is null)
            return;

         MembersToUpdate = optionsToInitializeFrom.MembersToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
      }
   }
}
