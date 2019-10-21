namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a temp table with 1 column.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   public sealed class TempTable<TColumn1>
   {
      /// <summary>
      /// Temp table column 1.
      /// </summary>
      public TColumn1 Column1 { get; set; }

#nullable disable
      /// <summary>
      /// Initializes <see cref="TempTable{TColumn1}"/>.
      /// </summary>
      // ReSharper disable once UnusedMember.Global
      public TempTable()
      {
      }
#nullable enable

      /// <summary>
      /// Initializes <see cref="TempTable{TColumn1}"/>.
      /// </summary>
      /// <param name="column1">Value of <see cref="Column1"/>.</param>
      public TempTable(TColumn1 column1)
      {
         Column1 = column1;
      }
   }

   /// <summary>
   /// Represents a temp table with 2 columns.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of column 2.</typeparam>
   public sealed class TempTable<TColumn1, TColumn2>
   {
      /// <summary>
      /// Temp table column 1.
      /// </summary>
      public TColumn1 Column1 { get; set; }

      /// <summary>
      /// Temp table column 2.
      /// </summary>
      public TColumn2 Column2 { get; set; }

#nullable disable
      /// <summary>
      /// Initializes <see cref="TempTable{TColumn1,TColumn2}"/>.
      /// </summary>
      // ReSharper disable once UnusedMember.Global
      public TempTable()
      {
      }
#nullable enable

      /// <summary>
      /// Initializes <see cref="TempTable{TColumn1,TColumn2}"/>.
      /// </summary>
      /// <param name="column1">Value of <see cref="Column1"/>.</param>
      /// <param name="column2">Value of <see cref="Column2"/>.</param>
      public TempTable(TColumn1 column1, TColumn2 column2)
      {
         Column1 = column1;
         Column2 = column2;
      }
   }
}
