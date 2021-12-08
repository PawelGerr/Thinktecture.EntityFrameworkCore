namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert or update.
/// </summary>
public interface ISqlServerBulkOperationOptions
{
   /// <summary>
   /// Options for creation of the temp table and for bulk insert of data used for later update.
   /// </summary>
   SqlServerTempTableBulkOperationOptions TempTableOptions { get; }

   /// <summary>
   /// Table hints for the MERGE command.
   /// </summary>
   List<SqlServerTableHintLimited> MergeTableHints { get; }
}
