using System;
using System.Collections.Generic;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public sealed class SqliteBulkInsertOptions : ISqliteBulkInsertOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqliteBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is null)
      {
         AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;
         return;
      }

      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is ISqliteBulkInsertOptions sqliteOptions)
         AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
   }
}