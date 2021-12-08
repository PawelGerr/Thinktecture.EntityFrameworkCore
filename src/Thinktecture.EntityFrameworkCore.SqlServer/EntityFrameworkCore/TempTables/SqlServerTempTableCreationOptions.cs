namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Options required for creation of a temp table.
/// </summary>
public class SqlServerTempTableCreationOptions : TempTableCreationOptions
{
   /// <summary>
   /// Adds "COLLATE database_default" to columns so the collation matches with the one of the user database instead of the master db.
   /// </summary>
   public bool UseDefaultDatabaseCollation { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableCreationOptions"/>.
   /// </summary>
   public SqlServerTempTableCreationOptions(ITempTableCreationOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom)
   {
      if (optionsToInitializeFrom is SqlServerTempTableCreationOptions sqlServerOptions)
         UseDefaultDatabaseCollation = sqlServerOptions.UseDefaultDatabaseCollation;
   }
}
