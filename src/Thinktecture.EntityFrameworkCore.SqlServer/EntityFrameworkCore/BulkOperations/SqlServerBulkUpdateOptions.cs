using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
      public SqlServerTempTableBulkInsertOptions TempTableOptions { get; }

      /// <inheritdoc />
      public IEntityMembersProvider? MembersToUpdate
      {
         get => TempTableOptions.MembersToInsert;
         set => TempTableOptions.MembersToInsert = value;
      }

      /// <inheritdoc />
      public IEntityMembersProvider? KeyProperties { get; set; }

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
            TempTableOptions = new SqlServerTempTableBulkInsertOptions(options.TempTableOptions);
            KeyProperties = options.KeyProperties;
            MergeTableHints = options.MergeTableHints;
         }
         else
         {
            TempTableOptions = new SqlServerTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };
         }
      }
   }
}
