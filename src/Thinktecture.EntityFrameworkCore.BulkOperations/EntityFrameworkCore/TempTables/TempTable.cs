namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a temp table with 1 column.
   /// </summary>
   /// <param name="Column1">1st column.</param>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   public record TempTable<TColumn1>(TColumn1 Column1);

   /// <summary>
   /// Represents a temp table with 2 columns.
   /// </summary>
   /// <param name="Column1">1st column.</param>
   /// <param name="Column2">2nd column.</param>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of column 2.</typeparam>
   public record TempTable<TColumn1, TColumn2>(TColumn1 Column1, TColumn2 Column2);
}
