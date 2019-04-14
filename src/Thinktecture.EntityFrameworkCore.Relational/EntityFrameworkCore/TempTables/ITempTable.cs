namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a temp table with 1 column.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   public interface ITempTable<TColumn1>
   {
      /// <summary>
      /// Temp table column.
      /// </summary>
      TColumn1 Column1 { get; set; }
   }

   /// <summary>
   /// Represents a temp table with 2 columns.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of column 2.</typeparam>
   public interface ITempTable<TColumn1, TColumn2>
   {
      /// <summary>
      /// Temp table column 1.
      /// </summary>
      TColumn1 Column1 { get; set; }

      /// <summary>
      /// Temp table column 2.
      /// </summary>
      TColumn2 Column2 { get; set; }
   }
}
