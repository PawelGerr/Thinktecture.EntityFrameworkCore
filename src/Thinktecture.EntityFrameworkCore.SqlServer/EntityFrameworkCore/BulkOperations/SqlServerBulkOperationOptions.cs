using System.Diagnostics.CodeAnalysis;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk operation options for SQL Server.
/// </summary>
public abstract class SqlServerBulkOperationOptions
{
   /// <summary>
   /// Properties to update.
   /// If the <see cref="PropertiesToUpdate"/> is <c>null</c> then all properties of the entity are going to be updated.
   /// </summary>
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <summary>
   /// Properties to perform the JOIN/match on.
   /// The primary key of the entity is used by default.
   /// </summary>
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Table hints for the MERGE command.
   /// </summary>
   [AllowNull]
   public List<SqlServerTableHintLimited> MergeTableHints { get; }

   /// <summary>
   /// Options for the temp table used by the 'MERGE' command.
   /// </summary>
   public SqlServerBulkOperationTempTableOptions TempTableOptions { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkOperationOptions"/>.
   /// </summary>
   /// <param name="options">Options to initialize from.</param>
   /// <param name="propertiesToUpdate">Properties to update.</param>
   /// <param name="keyProperties">Key properties.</param>
   protected SqlServerBulkOperationOptions(
      SqlServerBulkOperationOptions? options = null,
      IEntityPropertiesProvider? propertiesToUpdate = null,
      IEntityPropertiesProvider? keyProperties = null)
   {
      if (options is null)
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions();
         MergeTableHints = new List<SqlServerTableHintLimited> { SqlServerTableHintLimited.HoldLock };
      }
      else
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions(options.TempTableOptions);
         MergeTableHints = options.MergeTableHints;
      }

      KeyProperties = keyProperties;
      PropertiesToUpdate = propertiesToUpdate;
   }
}
