using System;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options for bulk insert into a temp table.
   /// </summary>
   public interface ITempTableBulkInsertOptions
   {
      /// <summary>
      /// Options for creation of the temp table.
      /// </summary>
      ITempTableCreationOptions TempTableCreationOptions { get; }

      /// <summary>
      /// Options for bulk insert.
      /// </summary>
      IBulkInsertOptions BulkInsertOptions { get; }

      /// <summary>
      /// Initializes current options using the provided <paramref name="options"/>.
      /// </summary>
      /// <param name="options">
      /// Options to be used for initialization of current instance.
      /// The <paramref name="options"/> may be of different type than the current instance.
      /// </param>
      /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
      /// <exception cref="NotSupportedException">Current instance cannot be initialized using the provided <paramref name="options"/>.</exception>
      void InitializeFrom(ITempTableBulkInsertOptions options);
   }
}
