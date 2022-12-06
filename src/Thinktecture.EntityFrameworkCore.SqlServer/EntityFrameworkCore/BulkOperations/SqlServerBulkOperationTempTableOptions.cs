using System.Data;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for the temp table used by the 'MERGE' command.
/// </summary>
public class SqlServerBulkOperationTempTableOptions
{
   internal SqlServerBulkOperationTempTableOptions(SqlServerBulkOperationTempTableOptions? options = null)
   {
      if (options is null)
      {
         PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;
         MomentOfPrimaryKeyCreation = MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert;
         DropTableOnDispose = true;
         EnableStreaming = true;
         DoNotUseDefaultValues = true;
      }
      else
      {
         PrimaryKeyCreation = options.PrimaryKeyCreation;
         MomentOfPrimaryKeyCreation = options.MomentOfPrimaryKeyCreation;
         TruncateTableIfExists = options.TruncateTableIfExists;
         DropTableOnDispose = options.DropTableOnDispose;
         TableNameProvider = options.TableNameProvider;
         PrimaryKeyCreation = options.PrimaryKeyCreation;
         BulkCopyTimeout = options.BulkCopyTimeout;
         SqlBulkCopyOptions = options.SqlBulkCopyOptions;
         BatchSize = options.BatchSize;
         EnableStreaming = options.EnableStreaming;
         UseDefaultDatabaseCollation = options.UseDefaultDatabaseCollation;
         DoNotUseDefaultValues = options.DoNotUseDefaultValues;
      }
   }

   /// <summary>
   /// Defines when the primary key should be created.
   /// Default is set to <see cref="MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert"/>.
   /// </summary>
   public MomentOfSqlServerPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; set; }

   /// <summary>
   /// Drops/truncates the temp table if the table exists already.
   /// Default is <c>false</c>.
   /// </summary>
   public bool TruncateTableIfExists { get; set; }

   /// <summary>
   /// Indication whether to drop the temp table on dispose of <see cref="ITempTableQuery{T}"/>.
   /// Default is <c>true</c>.
   /// </summary>
   /// <remarks>
   /// Set to <c>false</c> for more performance if the same temp table is re-used very often.
   /// Set <see cref="TruncateTableIfExists"/> to <c>true</c> on re-use.
   /// </remarks>
   public bool DropTableOnDispose { get; set; }

   /// <summary>
   /// Provides the name to create a temp table with.
   /// The default is the <see cref="ReusingTempTableNameProvider"/>.
   /// </summary>
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <summary>
   /// Provides the corresponding columns if the primary key should be created.
   /// The default is <see cref="PrimaryKeyPropertiesProviders.EntityTypeConfiguration"/>.
   /// </summary>
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

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

   /// <summary>
   /// Adds "COLLATE database_default" to columns so the collation matches with the one of the user database instead of the master db.
   /// </summary>
   public bool UseDefaultDatabaseCollation { get; set; }

   /// <summary>
   /// Do not use default values
   /// </summary>
   public bool DoNotUseDefaultValues { get; set; }

   internal void Populate(SqlServerTempTableBulkInsertOptions options)
   {
      options.BatchSize = BatchSize;
      options.EnableStreaming = EnableStreaming;
      options.BulkCopyTimeout = BulkCopyTimeout;
      options.PrimaryKeyCreation = PrimaryKeyCreation;
      options.TableNameProvider = TableNameProvider;
      options.TruncateTableIfExists = TruncateTableIfExists;
      options.DropTableOnDispose = DropTableOnDispose;
      options.SqlBulkCopyOptions = SqlBulkCopyOptions;
      options.UseDefaultDatabaseCollation = UseDefaultDatabaseCollation;
      options.MomentOfPrimaryKeyCreation = MomentOfPrimaryKeyCreation;
      options.DoNotUseDefaultValues = DoNotUseDefaultValues;
   }
}
