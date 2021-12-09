using System.Data;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into temp tables.
/// </summary>
public abstract class SqlServerTempTableBulkOperationOptions : ITempTableBulkInsertOptions
{
   /// <summary>
   /// Defines when the primary key should be created.
   /// Default is set to <see cref="MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert"/>.
   /// </summary>
   public MomentOfSqlServerPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; }

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
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

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Adds "COLLATE database_default" to columns so the collation matches with the one of the user database instead of the master db.
   /// </summary>
   public bool UseDefaultDatabaseCollation { get; set; }

   /// <summary>
   /// Advanced settings.
   /// </summary>
   public AdvancedSqlServerTempTableBulkOperationOptions Advanced { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableBulkOperationOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   protected SqlServerTempTableBulkOperationOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom)
   {
      Advanced = new AdvancedSqlServerTempTableBulkOperationOptions();

      if (optionsToInitializeFrom is null)
      {
         DropTableOnDispose = true;
         MomentOfPrimaryKeyCreation = MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert;
         EnableStreaming = true;
      }
      else
      {
         TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
         DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
         TableNameProvider = optionsToInitializeFrom.TableNameProvider;
         PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

         if (optionsToInitializeFrom is SqlServerTempTableBulkOperationOptions sqlServerOptions)
         {
            UseDefaultDatabaseCollation = sqlServerOptions.UseDefaultDatabaseCollation;

            BatchSize = sqlServerOptions.BatchSize;
            EnableStreaming = sqlServerOptions.EnableStreaming;
            BulkCopyTimeout = sqlServerOptions.BulkCopyTimeout;
            SqlBulkCopyOptions = sqlServerOptions.SqlBulkCopyOptions;
            MomentOfPrimaryKeyCreation = sqlServerOptions.MomentOfPrimaryKeyCreation;

            Advanced.UsePropertiesToInsertForTempTableCreation = sqlServerOptions.Advanced.UsePropertiesToInsertForTempTableCreation;
         }
      }
   }

   /// <summary>
   /// Gets options for creation of the temp table.
   /// </summary>
   public SqlServerTempTableCreationOptions GetTempTableCreationOptions()
   {
      return new SqlServerTempTableCreationOptions
             {
                TruncateTableIfExists = TruncateTableIfExists,
                DropTableOnDispose = DropTableOnDispose,
                TableNameProvider = TableNameProvider,
                PrimaryKeyCreation = PrimaryKeyCreation,
                UseDefaultDatabaseCollation = UseDefaultDatabaseCollation,

                PropertiesToInclude = Advanced.UsePropertiesToInsertForTempTableCreation ? PropertiesToInsert : null
             };
   }

   /// <summary>
   /// Gets options for bulk insert.
   /// </summary>
   public SqlServerBulkInsertOptions GetBulkInsertOptions()
   {
      return new SqlServerBulkInsertOptions
             {
                BatchSize = BatchSize,
                EnableStreaming = EnableStreaming,
                BulkCopyTimeout = BulkCopyTimeout,
                SqlBulkCopyOptions = SqlBulkCopyOptions,
                PropertiesToInsert = PropertiesToInsert
             };
   }

   /// <summary>
   /// Advanced options.
   /// </summary>
   public class AdvancedSqlServerTempTableBulkOperationOptions
   {
      /// <summary>
      /// By default all properties of the corresponding entity are used to create the temp table,
      /// i.e. the <see cref="SqlServerTempTableBulkOperationOptions.PropertiesToInsert"/> are ignored.
      /// This approach ensures that the returned <see cref="IQueryable{T}"/> doesn't throw an exception on read.
      /// The drawback is that all required (i.e. non-null) properties must be set accordingly and included into <see cref="SqlServerTempTableBulkOperationOptions.PropertiesToInsert"/>.
      ///
      /// If a use case requires the use of a subset of properties of the corresponding entity then set this setting to <c>true</c>.
      /// In this case the temp table is created by using <see cref="SqlServerTempTableBulkOperationOptions.PropertiesToInsert"/> only.
      /// The omitted properties must not be selected or used by the returned <see cref="IQueryable{T}"/>.
      /// 
      /// Default is <c>false</c>.
      /// </summary>
      public bool UsePropertiesToInsertForTempTableCreation { get; set; }
   }
}
