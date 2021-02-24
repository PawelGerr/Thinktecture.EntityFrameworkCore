using System.Collections.Immutable;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk update options.
   /// </summary>
   public interface ISqlServerBulkUpdateOptions : IBulkUpdateOptions
   {
      /// <summary>
      /// Options for creation of the temp table and for bulk insert of data used for later update.
      /// </summary>
      ISqlServerTempTableBulkInsertOptions TempTableOptions { get; }

      /// <summary>
      /// Table hints for the MERGE command.
      /// </summary>
      IImmutableList<TableHintLimited> MergeTableHints { get; set; }
   }
}
