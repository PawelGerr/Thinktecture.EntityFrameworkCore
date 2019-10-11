using System;
using System.Collections.Generic;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQLite.
   /// </summary>
   public class SqliteBulkInsertOptions : IBulkInsertOptions
   {
      /// <inheritdoc />
      public IEntityMembersProvider? EntityMembersProvider { get; set; }

      /// <summary>
      /// Behavior for auto-increment columns.
      /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
      /// </summary>
      public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; } = SqliteAutoIncrementBehavior.SetZeroToNull;

      /// <inheritdoc />
      public void InitializeFrom(IBulkInsertOptions options)
      {
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         EntityMembersProvider = options.EntityMembersProvider;

         if (options is SqliteBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
