using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Temp table options for PostgreSQL bulk update/upsert operations.
/// </summary>
public class NpgsqlBulkOperationTempTableOptions
{
   internal NpgsqlBulkOperationTempTableOptions(NpgsqlBulkOperationTempTableOptions? options = null)
   {
      if (options is null)
      {
         PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;
         MomentOfPrimaryKeyCreation = MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert;
         DropTableOnDispose = true;
      }
      else
      {
         PrimaryKeyCreation = options.PrimaryKeyCreation;
         MomentOfPrimaryKeyCreation = options.MomentOfPrimaryKeyCreation;
         TruncateTableIfExists = options.TruncateTableIfExists;
         DropTableOnDispose = options.DropTableOnDispose;
         TableNameProvider = options.TableNameProvider;
         CommandTimeout = options.CommandTimeout;
         Freeze = options.Freeze;
         SplitCollationComponents = options.SplitCollationComponents;
      }
   }

   /// <summary>
   /// Defines when the primary key should be created.
   /// Default is set to <see cref="MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert"/>.
   /// </summary>
   public MomentOfNpgsqlPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; set; }

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
   /// The default is <see cref="IPrimaryKeyPropertiesProvider.EntityTypeConfiguration"/>.
   /// </summary>
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

   /// <summary>
   /// Timeout for the COPY command.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// If <c>true</c>, appends the <c>FREEZE</c> option to the <c>COPY</c> command.
   /// Default is <c>false</c>.
   /// </summary>
   public bool Freeze { get; set; }

   /// <inheritdoc cref="NpgsqlTempTableCreationOptions.SplitCollationComponents" />
   public bool SplitCollationComponents { get; set; } = true;

   internal void Populate(NpgsqlTempTableBulkInsertOptions options)
   {
      options.CommandTimeout = CommandTimeout;
      options.Freeze = Freeze;
      options.PrimaryKeyCreation = PrimaryKeyCreation;
      options.TableNameProvider = TableNameProvider;
      options.TruncateTableIfExists = TruncateTableIfExists;
      options.DropTableOnDispose = DropTableOnDispose;
      options.MomentOfPrimaryKeyCreation = MomentOfPrimaryKeyCreation;
      options.SplitCollationComponents = SplitCollationComponents;
   }
}
