using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into temp tables for PostgreSQL.
/// </summary>
public sealed class NpgsqlTempTableBulkInsertOptions : ITempTableBulkInsertOptions
{
   /// <summary>
   /// Defines when the primary key should be created.
   /// Default is set to <see cref="MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert"/>.
   /// </summary>
   public MomentOfNpgsqlPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; }

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
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

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc cref="NpgsqlTempTableCreationOptions.SplitCollationComponents" />
   public bool SplitCollationComponents { get; set; } = true;

   /// <summary>
   /// Advanced settings.
   /// </summary>
   public AdvancedNpgsqlTempTableBulkInsertOptions Advanced { get; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlTempTableBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
   {
      Advanced = new AdvancedNpgsqlTempTableBulkInsertOptions();

      if (optionsToInitializeFrom is null)
      {
         DropTableOnDispose = true;
         MomentOfPrimaryKeyCreation = MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert;
      }
      else
      {
         TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
         DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
         TableNameProvider = optionsToInitializeFrom.TableNameProvider;
         PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

         if (optionsToInitializeFrom is NpgsqlTempTableBulkInsertOptions npgsqlOptions)
         {
            CommandTimeout = npgsqlOptions.CommandTimeout;
            Freeze = npgsqlOptions.Freeze;
            MomentOfPrimaryKeyCreation = npgsqlOptions.MomentOfPrimaryKeyCreation;
            SplitCollationComponents = npgsqlOptions.SplitCollationComponents;
            Advanced.UsePropertiesToInsertForTempTableCreation = npgsqlOptions.Advanced.UsePropertiesToInsertForTempTableCreation;
         }
      }
   }

   /// <summary>
   /// Gets options for creation of the temp table.
   /// </summary>
   public NpgsqlTempTableCreationOptions GetTempTableCreationOptions()
   {
      return new NpgsqlTempTableCreationOptions
             {
                TruncateTableIfExists = TruncateTableIfExists,
                DropTableOnDispose = DropTableOnDispose,
                TableNameProvider = TableNameProvider,
                PrimaryKeyCreation = PrimaryKeyCreation,
                SplitCollationComponents = SplitCollationComponents,
                PropertiesToInclude = Advanced.UsePropertiesToInsertForTempTableCreation ? PropertiesToInsert : null
             };
   }

   /// <summary>
   /// Gets options for bulk insert.
   /// </summary>
   public NpgsqlBulkInsertOptions GetBulkInsertOptions()
   {
      return new NpgsqlBulkInsertOptions
             {
                CommandTimeout = CommandTimeout,
                Freeze = Freeze,
                PropertiesToInsert = PropertiesToInsert
             };
   }

   /// <summary>
   /// Advanced options.
   /// </summary>
   public class AdvancedNpgsqlTempTableBulkInsertOptions
   {
      /// <summary>
      /// By default all properties of the corresponding entity are used to create the temp table,
      /// i.e. the <see cref="NpgsqlTempTableBulkInsertOptions.PropertiesToInsert"/> are ignored.
      /// This approach ensures that the returned <see cref="IQueryable{T}"/> doesn't throw an exception on read.
      /// The drawback is that all required (i.e. non-null) properties must be set accordingly and included into <see cref="NpgsqlTempTableBulkInsertOptions.PropertiesToInsert"/>.
      ///
      /// If a use case requires the use of a subset of properties of the corresponding entity then set this setting to <c>true</c>.
      /// In this case the temp table is created by using <see cref="NpgsqlTempTableBulkInsertOptions.PropertiesToInsert"/> only.
      /// The omitted properties must not be selected or used by the returned <see cref="IQueryable{T}"/>.
      ///
      /// Default is <c>false</c>.
      /// </summary>
      public bool UsePropertiesToInsertForTempTableCreation { get; set; }
   }
}
