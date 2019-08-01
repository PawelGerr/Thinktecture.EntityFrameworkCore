namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public class TempTableCreationOptions
   {
      /// <summary>
      /// Indication whether the table name should be unique.
      /// </summary>
      public bool MakeTableNameUnique { get; set; }
   }
}
