using System.Linq;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public sealed class SqliteTempTableBulkInsertOptions : ITempTableBulkInsertOptions
{
   /// <inheritdoc />
   public IBulkInsertOptions BulkInsertOptions => _bulkInsertOptions;

   private readonly SqliteBulkInsertOptions _bulkInsertOptions;

   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   public SqliteAutoIncrementBehavior AutoIncrementBehavior
   {
      get => _bulkInsertOptions.AutoIncrementBehavior;
      set => _bulkInsertOptions.AutoIncrementBehavior = value;
   }

   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; }

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert
   {
      get => _bulkInsertOptions.PropertiesToInsert;
      set => _bulkInsertOptions.PropertiesToInsert = value;
   }

   /// <summary>
   /// Advanced settings.
   /// </summary>
   public AdvancedSqliteTempTableBulkInsertOptions Advanced { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTempTableBulkInsertOptions"/>.
   /// </summary>
   public SqliteTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
   {
      _bulkInsertOptions = new SqliteBulkInsertOptions(optionsToInitializeFrom?.BulkInsertOptions);
      Advanced = new AdvancedSqliteTempTableBulkInsertOptions();

      if (optionsToInitializeFrom is null)
      {
         DropTableOnDispose = true;
      }
      else
      {
         TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
         DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
         TableNameProvider = optionsToInitializeFrom.TableNameProvider;
         PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

         if (optionsToInitializeFrom is SqliteTempTableBulkInsertOptions sqliteOptions)
            Advanced.UsePropertiesToInsertForTempTableCreation = sqliteOptions.Advanced.UsePropertiesToInsertForTempTableCreation;
      }
   }

   /// <summary>
   /// Options for creation of the temp table.
   /// </summary>
   public ITempTableCreationOptions GetTempTableCreationOptions()
   {
      return new TempTableCreationOptions
             {
                TruncateTableIfExists = TruncateTableIfExists,
                DropTableOnDispose = DropTableOnDispose,
                TableNameProvider = TableNameProvider,
                PrimaryKeyCreation = PrimaryKeyCreation,
                PropertiesToInclude = Advanced.UsePropertiesToInsertForTempTableCreation ? PropertiesToInsert : null
             };
   }

   /// <summary>
   /// Advanced options.
   /// </summary>
   public class AdvancedSqliteTempTableBulkInsertOptions
   {
      /// <summary>
      /// By default all properties of the corresponding entity are used to create the temp table,
      /// i.e. the <see cref="SqliteTempTableBulkInsertOptions.PropertiesToInsert"/> are ignored.
      /// This approach ensures that the returned <see cref="IQueryable{T}"/> doesn't throw an exception on read.
      /// The drawback is that all required (i.e. non-null) properties must be set accordingly and included into <see cref="SqliteTempTableBulkInsertOptions.PropertiesToInsert"/>.
      ///
      /// If a use case requires the use of a subset of properties of the corresponding entity then set this setting to <c>true</c>.
      /// In this case the temp table is created by using <see cref="SqliteTempTableBulkInsertOptions.PropertiesToInsert"/> only.
      /// The omitted properties must not be selected or used by the returned <see cref="IQueryable{T}"/>.
      /// 
      /// Default is <c>false</c>.
      /// </summary>
      public bool UsePropertiesToInsertForTempTableCreation { get; set; }
   }
}
