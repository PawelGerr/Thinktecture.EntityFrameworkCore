using System;
using System.Collections.Generic;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQLite.
   /// </summary>
   public sealed class SqliteBulkInsertOptions : IBulkInsertOptions
   {
      /// <inheritdoc />
      public IEntityMembersProvider? EntityMembersProvider { get; set; }

      /// <summary>
      /// Behavior for auto-increment columns.
      /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
      /// </summary>
      public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; } = SqliteAutoIncrementBehavior.SetZeroToNull;

      /// <summary>
      /// Initializes new instance of <see cref="SqliteBulkInsertOptions"/>.
      /// </summary>
      /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
      public SqliteBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
      {
         if (optionsToInitializeFrom is null)
            return;

         EntityMembersProvider = optionsToInitializeFrom.EntityMembersProvider;

         if (optionsToInitializeFrom is SqliteBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
