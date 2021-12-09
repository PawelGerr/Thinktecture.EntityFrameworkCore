namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for SQL Server 'MERGE' operation.
/// </summary>
public interface ISqlServerMergeOperationOptions
{
   /// <summary>
   /// Table hints for the MERGE command.
   /// </summary>
   List<SqlServerTableHintLimited> MergeTableHints { get; }

   /// <summary>
   /// Options for the temp table used by the 'MERGE' command.
   /// </summary>
   SqlServerBulkOperationTempTableOptions TempTableOptions { get; }
}
