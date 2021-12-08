using System.Data;
using Microsoft.Data.SqlClient;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQL Server.
/// </summary>
public sealed class SqlServerBulkInsertOptions : IBulkInsertOptions
{
   /// <summary>
   /// Timeout used by <see cref="SqlBulkCopy"/>
   /// </summary>
   public TimeSpan? BulkCopyTimeout { get; set; }

   /// <summary>
   /// Options used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public SqlBulkCopyOptions SqlBulkCopyOptions { get; set; }

   /// <summary>
   /// Batch size used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public int? BatchSize { get; set; }

   /// <summary>
   /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
   /// Default is set to <c>true</c>.
   /// </summary>
   public bool EnableStreaming { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
      EnableStreaming = true;

      if (optionsToInitializeFrom == null)
         return;

      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is SqlServerBulkInsertOptions sqlServerOptions)
      {
         BulkCopyTimeout = sqlServerOptions.BulkCopyTimeout;
         SqlBulkCopyOptions = sqlServerOptions.SqlBulkCopyOptions;
         BatchSize = sqlServerOptions.BatchSize;
         EnableStreaming = sqlServerOptions.EnableStreaming;
      }
   }
}
