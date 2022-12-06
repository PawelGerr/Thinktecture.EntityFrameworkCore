using System.Linq;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public sealed class SqliteTempTableBulkInsertOptions : ITempTableBulkInsertOptions
{
   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; }

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Advanced settings.
   /// </summary>
   public AdvancedSqliteTempTableBulkInsertOptions Advanced { get; }

   /// <inheritdoc />
   public bool DoNotUseDefaultValues { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTempTableBulkInsertOptions"/>.
   /// </summary>
   public SqliteTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
   {
      Advanced = new AdvancedSqliteTempTableBulkInsertOptions();

      if (optionsToInitializeFrom is null)
      {
         DropTableOnDispose = true;
         AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;
      }
      else
      {
         TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
         DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
         TableNameProvider = optionsToInitializeFrom.TableNameProvider;
         PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

         if (optionsToInitializeFrom is SqliteTempTableBulkInsertOptions sqliteOptions)
         {
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
            Advanced.UsePropertiesToInsertForTempTableCreation = sqliteOptions.Advanced.UsePropertiesToInsertForTempTableCreation;
         }
      }
   }

   /// <summary>
   /// Gets options for creation of the temp table.
   /// </summary>
   public TempTableCreationOptions GetTempTableCreationOptions()
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
   /// Get options for bulk insert.
   /// </summary>
   public SqliteBulkInsertOptions GetBulkInsertOptions()
   {
      return new SqliteBulkInsertOptions
             {
                AutoIncrementBehavior = AutoIncrementBehavior,
                PropertiesToInsert = PropertiesToInsert
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
