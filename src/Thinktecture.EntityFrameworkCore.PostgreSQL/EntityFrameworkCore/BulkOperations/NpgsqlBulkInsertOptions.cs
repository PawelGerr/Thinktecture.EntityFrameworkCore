namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for PostgreSQL.
/// </summary>
public sealed class NpgsqlBulkInsertOptions : IBulkInsertOptions
{
   /// <summary>
   /// Timeout for the COPY command.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// If <c>true</c>, appends the <c>FREEZE</c> option to the <c>COPY</c> command.
   /// Rows are frozen immediately, bypassing the normal MVCC visibility checks.
   /// This can significantly improve performance for bulk loads into tables that were created or truncated
   /// in the current transaction with no other concurrent readers.
   /// Default is <c>false</c>.
   /// </summary>
   public bool Freeze { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public string? TableName { get; set; }

   /// <inheritdoc />
   public string? Schema { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is null)
         return;

      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;
      TableName = optionsToInitializeFrom.TableName;
      Schema = optionsToInitializeFrom.Schema;

      if (optionsToInitializeFrom is NpgsqlBulkInsertOptions npgsqlOptions)
      {
         CommandTimeout = npgsqlOptions.CommandTimeout;
         Freeze = npgsqlOptions.Freeze;
      }
   }
}
