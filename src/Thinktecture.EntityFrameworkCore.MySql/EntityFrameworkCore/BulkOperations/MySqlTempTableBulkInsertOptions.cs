using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for MySQL bulk inserts into temporary tables.
/// </summary>
/// <remarks>
/// MySQL does not allow a temporary table to be referenced more than once in a single statement
/// (self-join, <c>EXISTS</c>, <c>UNION</c>, ...); doing so fails with error 1137 "Can't reopen table".
/// Insert the data into a second temp table or materialize the query instead.
/// Temp tables are also unavailable inside a <c>TransactionScope</c> (MySQL XA transactions reject them).
/// </remarks>
public sealed class MySqlTempTableBulkInsertOptions : ITempTableBulkInsertOptions
{
   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; } = true;

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Maximum number of rows per INSERT statement. The default is 500. Ignored when <see cref="UseBulkCopy" /> is enabled.
   /// </summary>
   public int? BatchSize { get; set; }

   /// <summary>
   /// Timeout of the generated commands. <c>null</c> uses the default of the connection.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// Uses <see cref="MySqlConnector.MySqlBulkCopy" /> (<c>LOAD DATA LOCAL INFILE</c>) instead of multi-row INSERT.
   /// The connection must enable <c>AllowLoadLocalInfile=true</c>, and the server must enable <c>local_infile</c>.
   /// This path buffers the input in memory before copying it.
   /// </summary>
   public bool UseBulkCopy { get; set; }

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlTempTableBulkInsertOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is null)
         return;

      TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
      DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
      TableNameProvider = optionsToInitializeFrom.TableNameProvider;
      PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is MySqlTempTableBulkInsertOptions mySqlOptions)
      {
         BatchSize = mySqlOptions.BatchSize;
         CommandTimeout = mySqlOptions.CommandTimeout;
         UseBulkCopy = mySqlOptions.UseBulkCopy;
      }
   }

   /// <summary>
   /// Creates options for the temporary-table DDL.
   /// </summary>
   /// <returns>The temp table creation options.</returns>
   public TempTableCreationOptions GetTempTableCreationOptions()
   {
      return new TempTableCreationOptions
             {
                TruncateTableIfExists = TruncateTableIfExists,
                DropTableOnDispose = DropTableOnDispose,
                TableNameProvider = TableNameProvider,
                PrimaryKeyCreation = PrimaryKeyCreation,
                PropertiesToInclude = PropertiesToInsert
             };
   }

   /// <summary>
   /// Creates options for the bulk insert.
   /// </summary>
   /// <returns>The bulk insert options.</returns>
   public MySqlBulkInsertOptions GetBulkInsertOptions()
   {
      return new MySqlBulkInsertOptions { PropertiesToInsert = PropertiesToInsert, BatchSize = BatchSize, CommandTimeout = CommandTimeout, UseBulkCopy = UseBulkCopy };
   }
}