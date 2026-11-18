using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL bulk insert options.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public sealed class MySqlBulkInsertOptions : IBulkInsertOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

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
   /// Initializes a new instance of <see cref="MySqlBulkInsertOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618
      PropertiesToInsert = optionsToInitializeFrom?.PropertiesToInsert;

      if (optionsToInitializeFrom is MySqlBulkInsertOptions mySqlOptions)
      {
         MaxParametersPerStatement = mySqlOptions.MaxParametersPerStatement;
         CommandTimeout = mySqlOptions.CommandTimeout;
      }
   }
}