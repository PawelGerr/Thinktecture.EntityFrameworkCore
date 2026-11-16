using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL bulk update options. The rows are staged in a temporary table and the target is updated with one <c>UPDATE ... INNER JOIN</c>.
/// The result is the number of matched rows, also rows whose values do not change. Duplicate keys in the input raise MySQL error 1062.
/// Duplicate input keys are not detected for keys with a NULL part and for TEXT, BLOB or JSON key columns, because the staging table has no
/// unique index that rejects them. In these cases MySQL updates a row with one of the duplicate rows.
/// The staging UPDATE statement runs with the optimizer hint <c>SET_VAR(sql_safe_updates = 0)</c>. On MariaDB and on MySQL before 8.0.3,
/// a session with <c>sql_safe_updates</c> gets error 1175.
/// MySQL rejects temporary tables inside an XA transaction, so the operation throws <see cref="NotSupportedException"/> inside a
/// <see cref="System.Transactions.TransactionScope"/> that uses XA (the default of MySqlConnector).
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public sealed class MySqlBulkUpdateOptions : IBulkUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Maximum number of parameters of one multi-row INSERT statement into the staging table; the number of rows per statement is this value divided by the number of columns, at least 1.
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

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well. For a temp table entity use 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)', because 'ForRootEntityWithDefaultSchema' throws for temp table entities.")]
   public string? TableName
   {
      get => _tableNames.TableName;
      set => _tableNames.TableName = value;
   }

   /// <inheritdoc />
   /// <remarks>MySQL handles the schema according to the <c>SchemaBehavior</c> of <c>UseMySql</c>: <c>MySqlSchemaBehavior.Throw</c> (the default) rejects it, <c>Ignore</c> drops it, and <c>Translate</c> passes it to the configured translator together with the table name.</remarks>
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(schema)' when only 'Schema' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'TableName' is set as well.")]
   public string? Schema
   {
      get => _tableNames.Schema;
      set => _tableNames.Schema = value;
   }

   /// <inheritdoc />
   public IBulkOperationTableNameProvider TableNameProvider
   {
      get => _tableNames.TableNameProvider;
      set => _tableNames.TableNameProvider = value;
   }

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlBulkUpdateOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618
      PropertiesToUpdate = optionsToInitializeFrom?.PropertiesToUpdate;
      KeyProperties = optionsToInitializeFrom?.KeyProperties;

      if (optionsToInitializeFrom is MySqlBulkUpdateOptions mySqlOptions)
      {
         MaxParametersPerStatement = mySqlOptions.MaxParametersPerStatement;
         CommandTimeout = mySqlOptions.CommandTimeout;
      }
   }
}