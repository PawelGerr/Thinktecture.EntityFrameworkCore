namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Options required for creation of a PostgreSQL temp table.
/// </summary>
public class NpgsqlTempTableCreationOptions : TempTableCreationOptions
{
   /// <summary>
   /// Indicates whether the collation string should be split on the first dot character
   /// to separate schema and collation name components before escaping them individually.
   /// When set to <c>false</c>, the entire collation string is escaped as a single identifier.
   /// Default is <c>true</c>.
   /// </summary>
   public bool SplitCollationComponents { get; set; } = true;

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlTempTableCreationOptions"/>.
   /// </summary>
   public NpgsqlTempTableCreationOptions(ITempTableCreationOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom)
   {
   }
}
