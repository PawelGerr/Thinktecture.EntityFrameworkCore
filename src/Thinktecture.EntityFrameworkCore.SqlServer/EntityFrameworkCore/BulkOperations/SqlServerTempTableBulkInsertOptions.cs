namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into temp tables.
/// </summary>
public sealed class SqlServerTempTableBulkInsertOptions : SqlServerTempTableBulkOperationOptions
{
   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom)
   {
   }
}
