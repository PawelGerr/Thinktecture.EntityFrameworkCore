using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk operation options for SQL Server.
   /// </summary>
   public abstract class SqlServerBulkOperationOptions
   {
      /// <summary>
      /// Options for creation of the temp table and for bulk insert of data used for later update.
      /// </summary>
      public SqlServerTempTableBulkOperationOptions TempTableOptions { get; }

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
      public List<TableHintLimited> MergeTableHints { get; }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerBulkOperationOptions"/>.
      /// </summary>
      /// <param name="options">Options to initialize from.</param>
      /// <param name="propertiesToUpdate">Properties to update.</param>
      /// <param name="keyProperties">Key properties.</param>
      protected SqlServerBulkOperationOptions(
         ISqlServerBulkOperationOptions? options = null,
         IEntityPropertiesProvider? propertiesToUpdate = null,
         IEntityPropertiesProvider? keyProperties = null)
      {
         if (options is null)
         {
            TempTableOptions = new ConcreteSqlServerTempTableBulkOperationOptions { PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };
            MergeTableHints = new List<TableHintLimited> { TableHintLimited.HoldLock };
         }
         else
         {
            TempTableOptions = new ConcreteSqlServerTempTableBulkOperationOptions(options.TempTableOptions);
            MergeTableHints = options.MergeTableHints;
         }

         KeyProperties = keyProperties;
         PropertiesToUpdate = propertiesToUpdate;
      }

      private class ConcreteSqlServerTempTableBulkOperationOptions : SqlServerTempTableBulkOperationOptions
      {
         public ConcreteSqlServerTempTableBulkOperationOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
            : base(optionsToInitializeFrom)
         {
         }
      }
   }
}
