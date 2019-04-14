using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a temp table with 1 column.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   public class TempTable<TColumn1> : ITempTable<TColumn1>
   {
      /// <inheritdoc />
      public TColumn1 Column1 { get; set; }

      /// <summary>
      /// Implicit conversion from value of type <typeparamref name="TColumn1"/> to <see cref="TempTable{TColumn1}"/>.
      /// </summary>
      /// <param name="columnValue">Value to convert.</param>
      /// <returns>An instance of <see cref="TempTable{TColumn1}"/>.</returns>
      [NotNull]
      public static implicit operator TempTable<TColumn1>(TColumn1 columnValue)
      {
         return new TempTable<TColumn1> { Column1 = columnValue };
      }
   }

   /// <summary>
   /// Represents a temp table with 2 columns.
   /// </summary>
   /// <typeparam name="TColumn1">Type of column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of column 2.</typeparam>
   public class TempTable<TColumn1, TColumn2> : ITempTable<TColumn1, TColumn2>
   {
      /// <inheritdoc />
      public TColumn1 Column1 { get; set; }

      /// <inheritdoc />
      public TColumn2 Column2 { get; set; }

      /// <summary>
      /// Implicit conversion from value of type <typeparamref name="TColumn1"/> to <see cref="TempTable{TColumn1,TColumn2}"/>.
      /// </summary>
      /// <param name="columnValues">Tuple containing values for columns.</param>
      /// <returns>An instance of <see cref="TempTable{TColumn1,TColumn2}"/>.</returns>
      [NotNull]
      public static implicit operator TempTable<TColumn1, TColumn2>((TColumn1 column1, TColumn2 column2) columnValues)
      {
         return new TempTable<TColumn1, TColumn2> { Column1 = columnValues.column1, Column2 = columnValues.column2 };
      }
   }
}
