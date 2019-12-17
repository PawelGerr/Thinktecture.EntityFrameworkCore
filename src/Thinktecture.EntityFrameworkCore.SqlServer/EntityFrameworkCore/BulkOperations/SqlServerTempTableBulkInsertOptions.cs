using System;
using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by the <see cref="BulkOperationsDbContextExtensions.BulkInsertIntoTempTableAsync{T}"/>.
   /// </summary>
   public sealed class SqlServerTempTableBulkInsertOptions : ITempTableBulkInsertOptions
   {
      IBulkInsertOptions ITempTableBulkInsertOptions.BulkInsertOptions => _bulkInsertOptions;
      ITempTableCreationOptions ITempTableBulkInsertOptions.TempTableCreationOptions => _tempTableCreationOptions;

      private readonly SqlServerBulkInsertOptions _bulkInsertOptions;
      private readonly TempTableCreationOptions _tempTableCreationOptions;

      private SqlServerPrimaryKeyCreation _primaryKeyCreation;

      /// <summary>
      /// Creates a clustered primary key spanning all columns of the temp table after the bulk insert.
      /// Default is set to <c>true</c>.
      /// </summary>
      public SqlServerPrimaryKeyCreation PrimaryKeyCreation
      {
         get => _primaryKeyCreation;
         set
         {
            _primaryKeyCreation = value;
            _tempTableCreationOptions.CreatePrimaryKey = value != SqlServerPrimaryKeyCreation.None;
         }
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
      /// Gets members to work with.
      /// </summary>
      /// <returns>A collection of <see cref="MemberInfo"/>.</returns>
      public IEntityMembersProvider? MembersToInsert
      {
         get => _bulkInsertOptions.MembersToInsert;
         set
         {
            _bulkInsertOptions.MembersToInsert = value;
            _tempTableCreationOptions.MembersToInclude = value;
         }
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerTempTableBulkInsertOptions"/>.
      /// </summary>
      public SqlServerTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
      {
         _bulkInsertOptions = new SqlServerBulkInsertOptions();
         _tempTableCreationOptions = new TempTableCreationOptions();

         PrimaryKeyCreation = SqlServerPrimaryKeyCreation.AfterBulkInsert;

         if (optionsToInitializeFrom != null)
            InitializeFrom(optionsToInitializeFrom);
      }

      private void InitializeFrom(ITempTableBulkInsertOptions options)
      {
         TruncateTableIfExists = options.TempTableCreationOptions.TruncateTableIfExists;
         DropTableOnDispose = options.TempTableCreationOptions.DropTableOnDispose;
         TableNameProvider = options.TempTableCreationOptions.TableNameProvider;
         MembersToInsert = options.BulkInsertOptions.MembersToInsert;

         if (options is SqlServerTempTableBulkInsertOptions sqlServerOptions)
         {
            PrimaryKeyCreation = sqlServerOptions.PrimaryKeyCreation;
            BatchSize = sqlServerOptions.BatchSize;
            EnableStreaming = sqlServerOptions.EnableStreaming;
            BulkCopyTimeout = sqlServerOptions.BulkCopyTimeout;
            SqlBulkCopyOptions = sqlServerOptions.SqlBulkCopyOptions;
         }
         else
         {
            PrimaryKeyCreation = options.TempTableCreationOptions.CreatePrimaryKey
                                    ? SqlServerPrimaryKeyCreation.AfterBulkInsert
                                    : SqlServerPrimaryKeyCreation.None;
         }
      }
   }
}
