namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table.
   /// </summary>
   public interface ISqlServerTempTableCreationOptions : ITempTableCreationOptions
   {
      /// <summary>
      /// Adds "COLLATE database_default" to columns so the collation matches with the one of the user database instead of the master db.
      /// </summary>
      bool UseDefaultDatabaseCollation { get; }
   }
}
