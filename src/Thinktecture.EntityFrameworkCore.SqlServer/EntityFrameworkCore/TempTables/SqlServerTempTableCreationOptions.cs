namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Options required for creation of a temp table.
/// </summary>
public class SqlServerTempTableCreationOptions : TempTableCreationOptions, ISqlServerTempTableCreationOptions
{
   /// <inheritdoc />
   public bool UseDefaultDatabaseCollation { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableCreationOptions"/>.
   /// </summary>
   public SqlServerTempTableCreationOptions(ITempTableCreationOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom)
   {
      if (optionsToInitializeFrom is ISqlServerTempTableCreationOptions sqlServerOptions)
         UseDefaultDatabaseCollation = sqlServerOptions.UseDefaultDatabaseCollation;
   }
}