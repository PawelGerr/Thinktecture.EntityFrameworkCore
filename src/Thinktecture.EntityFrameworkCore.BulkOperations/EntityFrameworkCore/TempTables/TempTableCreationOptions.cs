namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public class TempTableCreationOptions : ITempTableCreationOptions
   {
      /// <summary>
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      public bool MakeTableNameUnique { get; set; } = true;

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>true</c>.
      /// </summary>
      public bool CreatePrimaryKey { get; set; } = true;
   }
}
