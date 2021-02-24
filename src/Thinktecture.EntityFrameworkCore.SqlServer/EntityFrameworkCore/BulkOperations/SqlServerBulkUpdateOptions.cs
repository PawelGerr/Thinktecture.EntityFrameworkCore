using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQL Server.
   /// </summary>
   public sealed class SqlServerBulkUpdateOptions : ISqlServerBulkUpdateOptions
   {
      private static readonly ImmutableArray<TableHintLimited> _defaultTableHints = ImmutableArray<TableHintLimited>.Empty.Add(TableHintLimited.HoldLock);

      ISqlServerTempTableBulkInsertOptions ISqlServerBulkUpdateOptions.TempTableOptions => TempTableOptions;

      /// <summary>
      /// Options for creation of the temp table and for bulk insert of data used for later update.
      /// </summary>
      public SqlServerTempTableBulkOperationOptions TempTableOptions { get; }

      /// <inheritdoc />
      public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

      /// <inheritdoc />
      public IEntityPropertiesProvider? KeyProperties { get; set; }

      private IImmutableList<TableHintLimited>? _mergeTableHints;

      /// <inheritdoc />
      [AllowNull]
      public IImmutableList<TableHintLimited> MergeTableHints
      {
         get => _mergeTableHints ?? _defaultTableHints;
         set => _mergeTableHints = value ?? ImmutableList<TableHintLimited>.Empty;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
      /// </summary>
      /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
      public SqlServerBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
      {
         if (optionsToInitializeFrom is ISqlServerBulkUpdateOptions options)
         {
            TempTableOptions = new SqlServerTempTableBulkOperationOptions(options.TempTableOptions);
            KeyProperties = options.KeyProperties;
            MergeTableHints = options.MergeTableHints;
         }
         else
         {
            TempTableOptions = new SqlServerTempTableBulkOperationOptions { PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };
         }
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
      /// </summary>
      /// <param name="propertiesToUpdate">Provides properties to update.</param>
      /// <param name="keyProperties">Provides key properties.</param>
      public SqlServerBulkUpdateOptions(IEntityPropertiesProvider? propertiesToUpdate, IEntityPropertiesProvider? keyProperties)
      {
         TempTableOptions = new SqlServerTempTableBulkOperationOptions { PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };
         PropertiesToUpdate = propertiesToUpdate;
         KeyProperties = keyProperties;
      }
   }
}
