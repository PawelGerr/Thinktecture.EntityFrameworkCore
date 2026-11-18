using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for MySQL bulk inserts into temporary tables.
/// </summary>
/// <remarks>
/// MySQL does not allow a temporary table to be referenced more than once in a single statement
/// (self-join, <c>EXISTS</c>, <c>UNION</c>, ...); doing so fails with error 1137 "Can't reopen table".
/// Insert the data into a second temp table or materialize the query instead.
/// Inside an XA transaction (a <c>TransactionScope</c> with <c>UseXaTransactions=true</c>, the default) MySQL rejects the creation of a temporary table with error 4091,
/// unless the server runs with <c>xa_detach_on_prepare=OFF</c>. With <c>UseXaTransactions=false</c> temporary tables work inside a <c>TransactionScope</c>.
/// </remarks>
public sealed class MySqlTempTableBulkInsertOptions : ITempTableBulkInsertOptions
{
   /// <inheritdoc />
   public bool TruncateTableIfExists { get; set; }

   /// <inheritdoc />
   public bool DropTableOnDispose { get; set; } = true;

   /// <inheritdoc />
   public ITempTableNameProvider? TableNameProvider { get; set; }

   /// <inheritdoc />
   public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Maximum number of parameters of one multi-row INSERT statement; the number of rows per statement is this value divided by the number of columns, at least 1.
   /// <c>null</c> uses the value of the registration (<c>AddBulkOperationSupport</c>, 5,000 by default). The value is ignored if the registration enables bulk copy.
   /// </summary>
   /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or greater than 65,535.</exception>
   public int? MaxParametersPerStatement
   {
      get;
      set => field = value is null ? null : MySqlParameterBudget.Validate(value.Value, nameof(value));
   }

   /// <summary>
   /// Timeout of the generated commands. <c>null</c> uses the command timeout of the context.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// Advanced settings.
   /// </summary>
   public AdvancedMySqlTempTableBulkInsertOptions Advanced { get; } = new();

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlTempTableBulkInsertOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is null)
         return;

      TruncateTableIfExists = optionsToInitializeFrom.TruncateTableIfExists;
      DropTableOnDispose = optionsToInitializeFrom.DropTableOnDispose;
      TableNameProvider = optionsToInitializeFrom.TableNameProvider;
      PrimaryKeyCreation = optionsToInitializeFrom.PrimaryKeyCreation;
      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is MySqlTempTableBulkInsertOptions mySqlOptions)
      {
         MaxParametersPerStatement = mySqlOptions.MaxParametersPerStatement;
         CommandTimeout = mySqlOptions.CommandTimeout;
         Advanced.UsePropertiesToInsertForTempTableCreation = mySqlOptions.Advanced.UsePropertiesToInsertForTempTableCreation;
      }
   }

   /// <summary>
   /// Creates options for the temporary-table DDL.
   /// </summary>
   /// <returns>The temp table creation options.</returns>
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
   /// Creates options for the bulk insert.
   /// </summary>
   /// <returns>The bulk insert options.</returns>
   public MySqlBulkInsertOptions GetBulkInsertOptions()
   {
      return new MySqlBulkInsertOptions { PropertiesToInsert = PropertiesToInsert, MaxParametersPerStatement = MaxParametersPerStatement, CommandTimeout = CommandTimeout };
   }

   /// <summary>
   /// Advanced options.
   /// </summary>
   public sealed class AdvancedMySqlTempTableBulkInsertOptions
   {
      /// <summary>
      /// If <c>false</c>, the temp table is created with all properties of the corresponding entity, and <see cref="MySqlTempTableBulkInsertOptions.PropertiesToInsert"/> only selects the inserted columns.
      /// The returned <see cref="IQueryable{T}"/> can then read every property.
      /// However, every required (non-nullable) property must be part of <see cref="MySqlTempTableBulkInsertOptions.PropertiesToInsert"/>; otherwise the insert fails with MySQL error 1364.
      ///
      /// If <c>true</c>, the temp table is created with the properties of <see cref="MySqlTempTableBulkInsertOptions.PropertiesToInsert"/> only.
      /// The returned <see cref="IQueryable{T}"/> must not select or use the omitted properties.
      ///
      /// Default is <c>false</c>.
      /// </summary>
      public bool UsePropertiesToInsertForTempTableCreation { get; set; }
   }
}