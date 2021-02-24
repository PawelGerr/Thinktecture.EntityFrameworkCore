namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options for SQLite.
   /// </summary>
   public class SqliteBulkUpdateOptions : IBulkUpdateOptions
   {
      /// <inheritdoc />
      public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

      /// <inheritdoc />
      public IEntityPropertiesProvider? KeyProperties { get; set; }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteBulkUpdateOptions"/>.
      /// </summary>
      /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
      public SqliteBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
      {
         if (optionsToInitializeFrom is null)
            return;

         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
      }
   }
}
