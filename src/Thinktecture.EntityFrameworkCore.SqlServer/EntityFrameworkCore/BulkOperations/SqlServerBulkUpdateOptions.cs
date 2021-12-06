using System;
using System.Collections.Generic;
using System.Linq;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for SQL Server.
/// </summary>
public sealed class SqlServerBulkUpdateOptions : SqlServerBulkOperationOptions, ISqlServerBulkUpdateOptions
{
   ISqlServerTempTableBulkInsertOptions ISqlServerBulkOperationOptions.TempTableOptions => TempTableOptions;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom as ISqlServerBulkOperationOptions,
             optionsToInitializeFrom?.PropertiesToUpdate,
             optionsToInitializeFrom?.KeyProperties)
   {
   }
}