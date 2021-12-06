using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into temp tables.
/// </summary>
public abstract class SqlServerTempTableBulkOperationOptions : ISqlServerTempTableBulkInsertOptions
{
   IBulkInsertOptions ITempTableBulkInsertOptions.BulkInsertOptions => _bulkInsertOptions;
   ITempTableCreationOptions ITempTableBulkInsertOptions.TempTableCreationOptions => _tempTableCreationOptions;
   ISqlServerTempTableCreationOptions ISqlServerTempTableBulkInsertOptions.TempTableCreationOptions => _tempTableCreationOptions;
   IEntityPropertiesProvider? ISqlServerTempTableBulkInsertOptions.PropertiesToInsert => PropertiesToInsert;

   private readonly bool _couplePropertiesToInsertWithPropertiesToInclude;
   private readonly SqlServerBulkInsertOptions _bulkInsertOptions;
   private readonly SqlServerTempTableCreationOptions _tempTableCreationOptions;

   /// <inheritdoc />
   public MomentOfSqlServerPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public IPrimaryKeyPropertiesProvider PrimaryKeyCreation
   {
      get => _tempTableCreationOptions.PrimaryKeyCreation;
      set => _tempTableCreationOptions.PrimaryKeyCreation = value;
   }

   /// <summary>
   /// Drops/truncates the temp table if the table exists already.
   /// Default is <c>false</c>.
   /// </summary>
   public bool TruncateTableIfExists
   {
      get => _tempTableCreationOptions.TruncateTableIfExists;
      set => _tempTableCreationOptions.TruncateTableIfExists = value;
   }

   /// <summary>
   /// Indication whether to drop the temp table on dispose of <see cref="ITempTableQuery{T}"/>.
   /// Default is <c>true</c>.
   /// </summary>
   /// <remarks>
   /// Set to <c>false</c> for more performance if the same temp table is re-used very often.
   /// Set <see cref="TruncateTableIfExists"/> to <c>true</c> on re-use.
   /// </remarks>
   public bool DropTableOnDispose
   {
      get => _tempTableCreationOptions.DropTableOnDispose;
      set => _tempTableCreationOptions.DropTableOnDispose = value;
   }

   /// <summary>
   /// Provides the name to create a temp table with.
   /// </summary>

   public ITempTableNameProvider TableNameProvider
   {
      get => _tempTableCreationOptions.TableNameProvider;
      set => _tempTableCreationOptions.TableNameProvider = value;
   }

   /// <summary>
   /// Timeout used by <see cref="SqlBulkCopy"/>
   /// </summary>
   public TimeSpan? BulkCopyTimeout
   {
      get => _bulkInsertOptions.BulkCopyTimeout;
      set => _bulkInsertOptions.BulkCopyTimeout = value;
   }

   /// <summary>
   /// Options used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public SqlBulkCopyOptions SqlBulkCopyOptions
   {
      get => _bulkInsertOptions.SqlBulkCopyOptions;
      set => _bulkInsertOptions.SqlBulkCopyOptions = value;
   }

   /// <summary>
   /// Batch size used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public int? BatchSize
   {
      get => _bulkInsertOptions.BatchSize;
      set => _bulkInsertOptions.BatchSize = value;
   }

   /// <summary>
   /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
   /// Default is set to <c>true</c>.
   /// </summary>
   public bool EnableStreaming
   {
      get => _bulkInsertOptions.EnableStreaming;
      set => _bulkInsertOptions.EnableStreaming = value;
   }

   /// <summary>
   /// Gets properties to insert.
   /// </summary>
   protected IEntityPropertiesProvider? PropertiesToInsert
   {
      get => _bulkInsertOptions.PropertiesToInsert;
      set
      {
         _bulkInsertOptions.PropertiesToInsert = value;

         if (_couplePropertiesToInsertWithPropertiesToInclude)
            _tempTableCreationOptions.PropertiesToInclude = value;
      }
   }

   /// <summary>
   /// Adds "COLLATE database_default" to columns so the collation matches with the one of the user database instead of the master db.
   /// </summary>
   public bool UseDefaultDatabaseCollation
   {
      get => _tempTableCreationOptions.UseDefaultDatabaseCollation;
      set => _tempTableCreationOptions.UseDefaultDatabaseCollation = value;
   }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableBulkOperationOptions"/>.
   /// </summary>
   /// <param name="couplePropertiesToInsertWithPropertiesToInclude">
   /// Indication whether the <see cref="IEntityPropertiesProvider"/> of the <see cref="PropertiesToInsert"/> will be applied to <see cref="SqlServerTempTableCreationOptions"/>.</param>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   protected SqlServerTempTableBulkOperationOptions(
      bool couplePropertiesToInsertWithPropertiesToInclude,
      ITempTableBulkInsertOptions? optionsToInitializeFrom)
   {
      _couplePropertiesToInsertWithPropertiesToInclude = couplePropertiesToInsertWithPropertiesToInclude;
      _bulkInsertOptions = new SqlServerBulkInsertOptions();
      _tempTableCreationOptions = new SqlServerTempTableCreationOptions();

      MomentOfPrimaryKeyCreation = MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert;

      if (optionsToInitializeFrom != null)
         InitializeFrom(optionsToInitializeFrom);
   }

   private void InitializeFrom(ITempTableBulkInsertOptions options)
   {
      TruncateTableIfExists = options.TempTableCreationOptions.TruncateTableIfExists;
      DropTableOnDispose = options.TempTableCreationOptions.DropTableOnDispose;
      TableNameProvider = options.TempTableCreationOptions.TableNameProvider;
      PrimaryKeyCreation = options.TempTableCreationOptions.PrimaryKeyCreation;
      PropertiesToInsert = options.BulkInsertOptions.PropertiesToInsert;

      if (options is SqlServerTempTableBulkOperationOptions sqlServerOptions)
      {
         BatchSize = sqlServerOptions.BatchSize;
         EnableStreaming = sqlServerOptions.EnableStreaming;
         BulkCopyTimeout = sqlServerOptions.BulkCopyTimeout;
         SqlBulkCopyOptions = sqlServerOptions.SqlBulkCopyOptions;
         MomentOfPrimaryKeyCreation = sqlServerOptions.MomentOfPrimaryKeyCreation;
      }
   }
}