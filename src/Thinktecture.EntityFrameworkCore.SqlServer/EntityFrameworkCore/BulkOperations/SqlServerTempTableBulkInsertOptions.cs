using System;
using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by the <see cref="SqlServerDbContextExtensions.BulkInsertIntoTempTableAsync{T}"/>.
   /// </summary>
   public class SqlServerTempTableBulkInsertOptions
   {
      /// <summary>
      /// Options for bulk insert.
      /// </summary>
      internal SqlServerBulkInsertOptions BulkInsertOptions { get; }

      /// <summary>
      /// Options for creation of the temp table.
      /// Default is set to <c>true</c>.
      /// </summary>
      internal TempTableCreationOptions TempTableCreationOptions { get; }

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
            TempTableCreationOptions.CreatePrimaryKey = value == SqlServerPrimaryKeyCreation.BeforeBulkInsert;
         }
      }

      /// <summary>
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      public bool MakeTableNameUnique
      {
         get => TempTableCreationOptions.MakeTableNameUnique;
         set => TempTableCreationOptions.MakeTableNameUnique = value;
      }

      /// <summary>
      /// Timeout used by <see cref="SqlBulkCopy"/>
      /// </summary>
      public TimeSpan? BulkCopyTimeout
      {
         get => BulkInsertOptions.BulkCopyTimeout;
         set => BulkInsertOptions.BulkCopyTimeout = value;
      }

      /// <summary>
      /// Options used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      public SqlBulkCopyOptions SqlBulkCopyOptions
      {
         get => BulkInsertOptions.SqlBulkCopyOptions;
         set => BulkInsertOptions.SqlBulkCopyOptions = value;
      }

      /// <summary>
      /// Batch size used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      public int? BatchSize
      {
         get => BulkInsertOptions.BatchSize;
         set => BulkInsertOptions.BatchSize = value;
      }

      /// <summary>
      /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
      /// Default is set to <c>true</c>.
      /// </summary>
      public bool EnableStreaming
      {
         get => BulkInsertOptions.EnableStreaming;
         set => BulkInsertOptions.EnableStreaming = value;
      }

      /// <summary>
      /// Gets members to work with.
      /// </summary>
      /// <returns>A collection of <see cref="MemberInfo"/>.</returns>
      public IEntityMembersProvider? EntityMembersProvider
      {
         get => BulkInsertOptions.EntityMembersProvider;
         set => BulkInsertOptions.EntityMembersProvider = value;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerTempTableBulkInsertOptions"/>.
      /// </summary>
      public SqlServerTempTableBulkInsertOptions()
      {
         BulkInsertOptions = new SqlServerBulkInsertOptions();
         TempTableCreationOptions = new TempTableCreationOptions();
         PrimaryKeyCreation = SqlServerPrimaryKeyCreation.AfterBulkInsert;
      }
   }
}
