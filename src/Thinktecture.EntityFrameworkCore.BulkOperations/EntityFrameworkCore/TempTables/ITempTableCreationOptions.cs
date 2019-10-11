namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public interface ITempTableCreationOptions
   {
      /// <summary>
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      bool MakeTableNameUnique { get; }

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>false</c>.
      /// </summary>
      bool CreatePrimaryKey { get; }
   }
}
