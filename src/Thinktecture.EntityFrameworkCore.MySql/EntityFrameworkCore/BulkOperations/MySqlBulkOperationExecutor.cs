using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Transactions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Executes bulk operations for MySQL.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public sealed class MySqlBulkOperationExecutor : IBulkInsertExecutor, ITempTableBulkInsertExecutor, IBulkUpdateExecutor, IBulkInsertOrUpdateExecutor, ITruncateTableExecutor
{
   private const string _ORDER_COLUMN = "__ord";

   // With "sql_safe_updates" MySQL rejects the UPDATE and DELETE of the staging statements (error 1175), because the optimizer may scan a small table completely.
   // The join with the staging table limits the affected rows, so the setting is switched off for these statements only.
   private const string _SAFE_UPDATES_OFF_HINT = "/*+ SET_VAR(sql_safe_updates = 0) */";

   // MySQL rejects CREATE TEMPORARY TABLE inside an XA transaction with this error when xa_detach_on_prepare is ON (the default).
   private const int _XA_TEMPORARY_TABLE_ERROR = 4091;

   // CREATE TEMPORARY TABLE fails with this error when the session already has a table with the name.
   private const int _TABLE_EXISTS_ERROR = 1050;

   private readonly DbContext _context;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly IMySqlOptions _mySqlOptions;
   private readonly MySqlDbContextOptionsExtensionOptions _options;

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlBulkOperationExecutor" />.
   /// </summary>
   /// <param name="context">Current database context.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mapping source used to create command parameters.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   /// <param name="options">Options of the registration of the bulk operations (insert path and parameter budget).</param>
   public MySqlBulkOperationExecutor(
      ICurrentDbContext context,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      IMySqlOptions mySqlOptions,
      MySqlDbContextOptionsExtensionOptions options)
   {
      ArgumentNullException.ThrowIfNull(context);
      _mySqlOptions = mySqlOptions ?? throw new ArgumentNullException(nameof(mySqlOptions));
      _options = options ?? throw new ArgumentNullException(nameof(options));
      _context = context.Context ?? throw new ArgumentNullException(nameof(context));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public IBulkInsertOptions CreateOptions(IEntityPropertiesProvider? propertiesToInsert = null)
   {
      return new MySqlBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   IBulkInsertOptions ITempTableBulkInsertExecutor.CreateBulkInsertOptions(IEntityPropertiesProvider? propertiesToInsert)
   {
      return new MySqlBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   ITempTableBulkInsertOptions ITempTableBulkInsertExecutor.CreateOptions(IEntityPropertiesProvider? propertiesToInsert)
   {
      return new MySqlTempTableBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   IBulkUpdateOptions IBulkUpdateExecutor.CreateOptions(IEntityPropertiesProvider? propertiesToUpdate, IEntityPropertiesProvider? keyProperties)
   {
      return new MySqlBulkUpdateOptions
             {
                PropertiesToUpdate = propertiesToUpdate,
                KeyProperties = keyProperties
             };
   }

   /// <inheritdoc />
   IBulkInsertOrUpdateOptions IBulkInsertOrUpdateExecutor.CreateOptions(
      IEntityPropertiesProvider? propertiesToInsert,
      IEntityPropertiesProvider? propertiesToUpdate,
      IEntityPropertiesProvider? keyProperties)
   {
      return new MySqlBulkInsertOrUpdateOptions
             {
                PropertiesToInsert = propertiesToInsert,
                PropertiesToUpdate = propertiesToUpdate,
                KeyProperties = keyProperties
             };
   }

   /// <inheritdoc />
   public Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableBulkInsertOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      var mySqlOptions = options as MySqlTempTableBulkInsertOptions ?? new MySqlTempTableBulkInsertOptions(options);
      return BulkInsertIntoTempTableAsync<T, T>(entities, mySqlOptions, static query => query, cancellationToken);
   }

   /// <inheritdoc />
   public Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
      IEnumerable<TColumn1> values,
      ITempTableBulkInsertOptions options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(values);
      ArgumentNullException.ThrowIfNull(options);

      var mySqlOptions = options as MySqlTempTableBulkInsertOptions ?? new MySqlTempTableBulkInsertOptions(options);
      var entities = values.Select(value => new TempTable<TColumn1>(value));
      return BulkInsertIntoTempTableAsync<TempTable<TColumn1>, TColumn1>(entities, mySqlOptions, static query => query.Select(row => row.Column1), cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertValuesIntoTempTableAsync<TColumn1>(
      IEnumerable<TColumn1> values,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(values);
      ArgumentNullException.ThrowIfNull(tempTable);
      var mySqlOptions = options as MySqlTempTableBulkInsertOptions ?? new MySqlTempTableBulkInsertOptions(options);
      return BulkInsertValuesIntoTempTableAsync(values, tempTable, mySqlOptions.GetBulkInsertOptions(), cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertValuesIntoTempTableAsync<TColumn1>(
      IEnumerable<TColumn1> values,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(values);
      ArgumentNullException.ThrowIfNull(tempTable);
      return BulkInsertIntoExistingTempTableAsync(values.Select(value => new TempTable<TColumn1>(value)), tempTable, options, cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertIntoTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(tempTable);
      var mySqlOptions = options as MySqlTempTableBulkInsertOptions ?? new MySqlTempTableBulkInsertOptions(options);
      return BulkInsertIntoExistingTempTableAsync(entities, tempTable, mySqlOptions.GetBulkInsertOptions(), cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertIntoTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      CancellationToken cancellationToken)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(tempTable);
      return BulkInsertIntoExistingTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <inheritdoc />
   public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, IBulkInsertOptions options, CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      var mySqlOptions = options as MySqlBulkInsertOptions ?? new MySqlBulkInsertOptions(options);
      var entityType = _context.Model.GetEntityTypeOrTempTableEntityType<T>();
      var properties = mySqlOptions.PropertiesToInsert.DeterminePropertiesForInsert(entityType, null);
      properties.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();

      // Every table is resolved once and before the first statement, so no row of the owner is written when the table of an owned type is invalid.
      // Inlined owned types are skipped, because the root-only providers throw for every owned type.
      var tables = new Dictionary<IEntityType, BulkOperationTableName> { [entityType] = GetTableName(mySqlOptions.TableNameProvider, entityType) };

      foreach (var ownedType in properties.SelectMany(property => property.Navigations)
                                          .Where(navigation => navigation.TargetEntityType.IsOwned() && !navigation.IsInlined())
                                          .Select(navigation => navigation.TargetEntityType)
                                          .Distinct())
      {
         tables[ownedType] = GetTableName(mySqlOptions.TableNameProvider, ownedType);
      }

      var resolvedTables = IBulkOperationTableNameProvider.Create(type => tables[type]);

      return await ExecuteInTransactionAsync(token => InsertAsync(entityType, entities, properties, mySqlOptions, resolvedTables, token), cancellationToken).ConfigureAwait(false);
   }

   /// <inheritdoc />
   public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, IBulkUpdateOptions options, CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      var mySqlOptions = options as MySqlBulkUpdateOptions ?? new MySqlBulkUpdateOptions(options);
      var entityType = _context.Model.GetEntityTypeOrTempTableEntityType<T>();

      // Resolved first: a temp table entity without a table name is reported as such, not as a missing key.
      var table = GetTableName(mySqlOptions.TableNameProvider, entityType);
      var propertiesToUpdate = mySqlOptions.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, null);
      var keyProperties = mySqlOptions.KeyProperties.DetermineKeyProperties(entityType);

      propertiesToUpdate.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();
      EnsureNoOwnedTypes(entityType, propertiesToUpdate);

      var propertiesToSet = propertiesToUpdate.Where(property => !keyProperties.Contains(property.Property)).ToList();

      if (propertiesToSet.Count == 0)
         throw new ArgumentException("The number of properties to update cannot be 0.");

      var keys = ToPropertiesWithNavigations(keyProperties);
      var stagingProperties = keys.Concat(propertiesToSet).ToList();
      var staging = new StagingTable(entityType, table, keys, stagingProperties, mySqlOptions.MaxParametersPerStatement, mySqlOptions.CommandTimeout, false);

      return await ExecuteInTransactionAsync(token => ExecuteWithStagingTableAsync(staging,
                                                                                   entities,
                                                                                   (stagingTable, innerToken) => ExecuteSqlAsync(CreateUpdateStatement(table, stagingTable, propertiesToSet, keys), innerToken),
                                                                                   token),
                                             cancellationToken).ConfigureAwait(false);
   }

   /// <inheritdoc />
   public async Task<int> BulkInsertOrUpdateAsync<T>(IEnumerable<T> entities, IBulkInsertOrUpdateOptions options, CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      var mySqlOptions = options as MySqlBulkInsertOrUpdateOptions ?? new MySqlBulkInsertOrUpdateOptions(options);
      var entityType = _context.Model.GetEntityTypeOrTempTableEntityType<T>();

      // Resolved first: a temp table entity without a table name is reported as such, not as a missing key.
      var table = GetTableName(mySqlOptions.TableNameProvider, entityType);
      var propertiesToInsert = mySqlOptions.PropertiesToInsert.DeterminePropertiesForInsert(entityType, true);
      var propertiesToUpdate = mySqlOptions.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, true);
      var keyProperties = mySqlOptions.KeyProperties.DetermineKeyProperties(entityType);

      EnsureNoOwnedTypes(entityType, [.. propertiesToInsert, .. propertiesToUpdate]);

      if (propertiesToInsert.Count == 0)
         throw new ArgumentException("The number of properties to insert cannot be 0.");

      // The primary key is never rewritten by default, even if other properties are used as the upsert key.
      IReadOnlyList<IProperty> primaryKeyProperties = mySqlOptions.PropertiesToUpdate is null
                                                         ? entityType.FindPrimaryKey()?.Properties ?? []
                                                         : [];
      var propertiesToSet = propertiesToUpdate.Where(property => !keyProperties.Contains(property.Property) && !primaryKeyProperties.Contains(property.Property)).ToList();
      var keys = ToPropertiesWithNavigations(keyProperties);
      var stagingProperties = keys.Concat(propertiesToInsert).Concat(propertiesToSet).Distinct().ToList();
      var staging = new StagingTable(entityType, table, keys, stagingProperties, mySqlOptions.MaxParametersPerStatement, mySqlOptions.CommandTimeout, true);

      return await ExecuteInTransactionAsync(token => ExecuteWithStagingTableAsync(staging,
                                                                                   entities,
                                                                                   (stagingTable, innerToken) => UpsertFromStagingTableAsync(staging, stagingTable, propertiesToInsert, propertiesToSet, innerToken),
                                                                                   token),
                                             cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Performs a bulk update using a server-side query as the source.
   /// </summary>
   /// <typeparam name="TTarget">Type of the entity to update.</typeparam>
   /// <typeparam name="TSource">Type of the source query's entity.</typeparam>
   /// <typeparam name="TResult">Type of the join key.</typeparam>
   /// <param name="sourceQuery">Query providing the source rows.</param>
   /// <param name="targetKeySelector">Selects the key of the target entity.</param>
   /// <param name="sourceKeySelector">Selects the key of the source entity.</param>
   /// <param name="setPropertyCalls">Configures the property assignments.</param>
   /// <param name="filter">Optional condition restricting the updated rows.</param>
   /// <param name="options">Optional options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   public async Task<int> BulkUpdateAsync<TTarget, TSource, TResult>(
      IQueryable<TSource> sourceQuery,
      Expression<Func<TTarget, TResult?>> targetKeySelector,
      Expression<Func<TSource, TResult?>> sourceKeySelector,
      Func<SetPropertyBuilder<TTarget, TSource>, SetPropertyBuilder<TTarget, TSource>> setPropertyCalls,
      Expression<Func<TTarget, TSource, bool>>? filter = null,
      MySqlBulkUpdateFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(targetKeySelector);
      ArgumentNullException.ThrowIfNull(sourceKeySelector);
      ArgumentNullException.ThrowIfNull(setPropertyCalls);

      var setClauseBuilder = setPropertyCalls(new SetPropertyBuilder<TTarget, TSource>());

      if (setClauseBuilder.Entries.Count == 0)
         throw new ArgumentException("At least one property assignment is required.", nameof(setPropertyCalls));

      var targetKeyMembers = targetKeySelector.ExtractMembers();
      var sourceKeyMembers = sourceKeySelector.ExtractMembers();

      if (targetKeyMembers.Count != sourceKeyMembers.Count)
         throw new ArgumentException($"The number of target key properties ({targetKeyMembers.Count}) must match the number of source key properties ({sourceKeyMembers.Count}).");

      var translator = new EfCoreValueExpressionTranslator(_context);
      var (command, parameterValues) = translator.TranslateUpdateFromQuery(sourceQuery,
                                                                           targetKeySelector,
                                                                           sourceKeySelector,
                                                                           setClauseBuilder.Entries,
                                                                           filter,
                                                                           CreateSchemaCheckingProvider(options?.TableNameProvider));

      return await translator.ExecuteNonQueryAsync(command, parameterValues, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Performs a bulk insert using a server-side query as the source.
   /// </summary>
   /// <typeparam name="TTarget">Type of the entity to insert.</typeparam>
   /// <typeparam name="TSource">Type of the source query's entity.</typeparam>
   /// <param name="sourceQuery">Query providing the source rows.</param>
   /// <param name="mapPropertyCalls">Configures the column mappings.</param>
   /// <param name="options">Optional options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   public async Task<int> BulkInsertAsync<TTarget, TSource>(
      IQueryable<TSource> sourceQuery,
      Func<InsertPropertyBuilder<TTarget, TSource>, InsertPropertyBuilder<TTarget, TSource>> mapPropertyCalls,
      MySqlBulkInsertFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(mapPropertyCalls);

      var builder = mapPropertyCalls(new InsertPropertyBuilder<TTarget, TSource>());

      if (builder.Entries.Count == 0)
         throw new ArgumentException("At least one column mapping is required.", nameof(mapPropertyCalls));

      var translator = new EfCoreValueExpressionTranslator(_context);
      var (command, parameterValues) = translator.TranslateInsertFromQuery<TTarget, TSource>(sourceQuery,
                                                                                             builder.Entries,
                                                                                             CreateSchemaCheckingProvider(options?.TableNameProvider));

      return await translator.ExecuteNonQueryAsync(command, parameterValues, cancellationToken).ConfigureAwait(false);
   }

   /// <inheritdoc />
   /// <remarks>
   /// Throws when a transaction is active, because <c>TRUNCATE TABLE</c> commits it implicitly in MySQL.
   /// This method is the same as <see cref="TruncateTableAsync{T}(bool, CancellationToken)"/> with <c>throwOnTransaction: true</c>.
   /// </remarks>
   public Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
      where T : class
   {
      return TruncateTableAsync(typeof(T), true, cancellationToken);
   }

   /// <inheritdoc />
   /// <remarks>
   /// Throws when a transaction is active, because <c>TRUNCATE TABLE</c> commits it implicitly in MySQL.
   /// This method is the same as <see cref="TruncateTableAsync(Type, bool, CancellationToken)"/> with <c>throwOnTransaction: true</c>.
   /// </remarks>
   public Task TruncateTableAsync(Type type, CancellationToken cancellationToken = default)
   {
      return TruncateTableAsync(type, true, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/> with <c>TRUNCATE TABLE</c>.
   /// </summary>
   /// <param name="throwOnTransaction">
   /// If <c>true</c>, the method throws when a transaction is active.
   /// If <c>false</c>, <c>TRUNCATE TABLE</c> is executed inside the active transaction, and MySQL commits this transaction implicitly.
   /// </param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   /// <returns>A task that completes when the table is truncated.</returns>
   /// <remarks>
   /// <para>
   /// The method detects the current transaction of the context, an ambient transaction (<see cref="Transaction.Current"/>) and a transaction that the context is enlisted in.
   /// It cannot detect the following transactions, and in these cases <c>TRUNCATE TABLE</c> commits them implicitly without an error:
   /// a transaction that was started with a raw <c>START TRANSACTION</c> or by <c>autocommit=0</c>,
   /// a connection that was enlisted with <see cref="System.Data.Common.DbConnection.EnlistTransaction"/> directly while <c>UseXaTransactions=false</c>,
   /// and a <see cref="TransactionScope"/> without <see cref="TransactionScopeAsyncFlowOption.Enabled"/> after a switch of the thread.
   /// A transaction of <see cref="System.Data.Common.DbConnection.BeginTransaction()"/> that was not passed to <c>UseTransaction</c> is not committed silently,
   /// because MySqlConnector rejects the command unless <c>IgnoreCommandTransaction=true</c> is set.
   /// </para>
   /// <para>
   /// MySQL rejects <c>TRUNCATE TABLE</c> of a table that a foreign key of another table references with error 1701, also when the referencing table is empty.
   /// Inside an XA transaction (a <see cref="TransactionScope"/> with <c>UseXaTransactions=true</c>, the default) MySQL rejects it with error 1399.
   /// </para>
   /// </remarks>
   /// <exception cref="InvalidOperationException">The parameter <paramref name="throwOnTransaction"/> is <c>true</c> and a transaction is active.</exception>
   /// <exception cref="NotSupportedException">The table has a schema and the schema behavior of the provider is <c>Throw</c>.</exception>
   public Task TruncateTableAsync<T>(bool throwOnTransaction, CancellationToken cancellationToken = default)
      where T : class
   {
      return TruncateTableAsync(typeof(T), throwOnTransaction, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/> with <c>TRUNCATE TABLE</c>.
   /// </summary>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="throwOnTransaction">
   /// If <c>true</c>, the method throws when a transaction is active.
   /// If <c>false</c>, <c>TRUNCATE TABLE</c> is executed inside the active transaction, and MySQL commits this transaction implicitly.
   /// </param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A task that completes when the table is truncated.</returns>
   /// <remarks>
   /// <para>
   /// The method detects the current transaction of the context, an ambient transaction (<see cref="Transaction.Current"/>) and a transaction that the context is enlisted in.
   /// It cannot detect the following transactions, and in these cases <c>TRUNCATE TABLE</c> commits them implicitly without an error:
   /// a transaction that was started with a raw <c>START TRANSACTION</c> or by <c>autocommit=0</c>,
   /// a connection that was enlisted with <see cref="System.Data.Common.DbConnection.EnlistTransaction"/> directly while <c>UseXaTransactions=false</c>,
   /// and a <see cref="TransactionScope"/> without <see cref="TransactionScopeAsyncFlowOption.Enabled"/> after a switch of the thread.
   /// A transaction of <see cref="System.Data.Common.DbConnection.BeginTransaction()"/> that was not passed to <c>UseTransaction</c> is not committed silently,
   /// because MySqlConnector rejects the command unless <c>IgnoreCommandTransaction=true</c> is set.
   /// </para>
   /// <para>
   /// MySQL rejects <c>TRUNCATE TABLE</c> of a table that a foreign key of another table references with error 1701, also when the referencing table is empty.
   /// Inside an XA transaction (a <see cref="TransactionScope"/> with <c>UseXaTransactions=true</c>, the default) MySQL rejects it with error 1399.
   /// </para>
   /// </remarks>
   /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">The parameter <paramref name="throwOnTransaction"/> is <c>true</c> and a transaction is active.</exception>
   /// <exception cref="NotSupportedException">The table has a schema and the schema behavior of the provider is <c>Throw</c>.</exception>
   public async Task TruncateTableAsync(Type type, bool throwOnTransaction, CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(type);

      if (throwOnTransaction && HasTransaction)
         throw new InvalidOperationException("TRUNCATE TABLE commits the current transaction in MySQL. Complete the transaction first, or call the MySQL overload with 'throwOnTransaction: false'.");

      var entityType = _context.Model.GetEntityType(type);
      var tableName = entityType.GetTableName()
                      ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var schema = entityType.GetSchema();
      EnsureSchemaIsSupported(new BulkOperationTableName(tableName, schema));

      var sql = $"TRUNCATE TABLE {_sqlGenerationHelper.DelimitIdentifier(tableName, schema)};";

      await ExecuteSqlAsync(sql, cancellationToken).ConfigureAwait(false);
   }

   private BulkOperationTableName GetTableName(IBulkOperationTableNameProvider tableNameProvider, IEntityType entityType)
   {
      var table = tableNameProvider.GetValidTableName(entityType);
      EnsureSchemaIsSupported(table);
      return table;
   }

   private IBulkOperationTableNameProvider CreateSchemaCheckingProvider(IBulkOperationTableNameProvider? tableNameProvider)
   {
      tableNameProvider ??= IBulkOperationTableNameProvider.Default;
      return IBulkOperationTableNameProvider.Create(entityType => GetTableName(tableNameProvider, entityType));
   }

   // With SchemaBehavior Throw the provider has no translator and DelimitIdentifier drops the schema, so the statement would address the current database.
   // Ignore and Translate set a translator, which DelimitIdentifier applies.
   private void EnsureSchemaIsSupported(BulkOperationTableName table)
   {
      if (!String.IsNullOrEmpty(table.Schema) && _mySqlOptions.SchemaNameTranslator is null)
         throw new NotSupportedException($"The table '{table.Name}' has the schema '{table.Schema}', but MySQL has no schemas. Set 'SchemaBehavior' in 'UseMySql' to 'MySqlSchemaBehavior.Ignore' or 'MySqlSchemaBehavior.Translate', or remove the schema.");
   }

   // The staging table has the columns of the root table only, so bulk update and upsert reject every kind of owned type, like SQL Server and PostgreSQL.
   private static void EnsureNoOwnedTypes(IEntityType entityType, IReadOnlyList<PropertyWithNavigations> properties)
   {
      if (HasSeparateOwnedType(entityType, entityType.GetTableName(), entityType.GetSchema()))
         throw new NotSupportedException("MySQL bulk update and upsert do not support owned entities mapped to separate tables.");

      if (properties.Any(property => property.Navigations.Count != 0))
         throw new NotSupportedException("Temp tables don't support owned entities.");
   }

   // Owned types nested in inlined owned types are checked as well.
   private static bool HasSeparateOwnedType(IEntityType entityType, string? tableName, string? schema)
   {
      return entityType.GetNavigations().Any(navigation => navigation.TargetEntityType.IsOwned()
                                                           && (navigation.TargetEntityType.GetTableName() != tableName
                                                               || navigation.TargetEntityType.GetSchema() != schema
                                                               || HasSeparateOwnedType(navigation.TargetEntityType, tableName, schema)));
   }

   private async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<TEntity, T>(
      IEnumerable<TEntity> entities,
      MySqlTempTableBulkInsertOptions options,
      Func<IQueryable<TEntity>, IQueryable<T>> projection,
      CancellationToken cancellationToken)
      where TEntity : class
   {
      var entityTypeName = EntityNameProvider.GetTempTableName(typeof(TEntity));
      var entityType = _context.Model.GetEntityType(entityTypeName, typeof(TEntity));
      var tempTableReference = await _context.GetService<ITempTableCreator>()
                                             .CreateTempTableAsync(entityType, options.GetTempTableCreationOptions(), cancellationToken)
                                             .ConfigureAwait(false);

      try
      {
         var insertOptions = options.GetBulkInsertOptions();
         insertOptions.TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tempTableReference.Name);
         var insertedRows = await BulkInsertAsync(entities, insertOptions, cancellationToken).ConfigureAwait(false);

         // Regular (not temp-table-registered) entities are queried via their own entity type.
         var dbSet = entityType.Name == entityTypeName
                        ? _context.Set<TEntity>(entityTypeName)
                        : _context.Set<TEntity>();
         var query = dbSet.FromTempTable(new TempTableInfo(tempTableReference.Name));

         if (entityType.FindPrimaryKey() is { Properties.Count: > 0 })
            query = query.AsNoTracking();

         return new TempTableQuery<T>(projection(query), tempTableReference, insertedRows);
      }
      catch
      {
         await tempTableReference.DisposeAsync().ConfigureAwait(false);
         throw;
      }
   }

   private Task BulkInsertIntoExistingTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      CancellationToken cancellationToken)
      where T : class
   {
      // Copy: the caller's options must not be redirected to the temp table.
      var insertOptions = new MySqlBulkInsertOptions(options)
                          {
                             TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tempTable.Name)
                          };
      return BulkInsertAsync(entities, insertOptions, cancellationToken);
   }

   private static string[] GetColumnNames(IReadOnlyList<PropertyWithNavigations> properties)
   {
      StoreObjectIdentifier? storeObject = null;
      var columnNames = new string[properties.Count];

      for (var i = 0; i < properties.Count; i++)
      {
         var property = properties[i];
         storeObject ??= property.GetStoreObject();
         columnNames[i] = property.Property.GetColumnName(storeObject.Value)
                          ?? throw new InvalidOperationException($"The property '{property.Property.Name}' has no column name.");
      }

      return columnNames;
   }

   private int GetRowsPerStatement(int? maxParametersPerStatement, int numberOfColumns)
   {
      return Math.Max(1, (maxParametersPerStatement ?? _options.MaxParametersPerStatement) / numberOfColumns);
   }

   // The data reader yields provider values (value converters already applied), so the mapping must describe the provider type.
   private RelationalTypeMapping?[] GetProviderMappings(IReadOnlyList<PropertyWithNavigations> properties)
   {
      var mappings = new RelationalTypeMapping?[properties.Count];

      for (var i = 0; i < mappings.Length; i++)
      {
         var property = properties[i].Property;
         var mapping = property.GetRelationalTypeMapping();
         var converter = property.GetValueConverter() ?? mapping.Converter;

         mappings[i] = converter is null
                          ? mapping
                          : _typeMappingSource.FindMapping(converter.ProviderClrType, property.GetColumnType());
      }

      return mappings;
   }

   private bool HasTransaction => _context.Database.CurrentTransaction is not null
                                  || Transaction.Current is not null
                                  || _context.Database.GetEnlistedTransaction() is not null;

   private async Task<int> ExecuteInTransactionAsync(Func<CancellationToken, Task<int>> action, CancellationToken cancellationToken)
   {
      await _context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

      try
      {
         // Without an outer transaction a failing batch would leave the earlier batches committed.
         // An ambient TransactionScope already provides atomicity and EF refuses to start its own transaction inside it.
         await using var transaction = HasTransaction
                                          ? null
                                          : await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

         var result = await action(cancellationToken).ConfigureAwait(false);

         if (transaction is not null)
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

         return result;
      }
      finally
      {
         await _context.Database.CloseConnectionAsync().ConfigureAwait(false);
      }
   }

   private DbCommand CreateCommand(TimeSpan? commandTimeout)
   {
      var command = _context.Database.GetDbConnection().CreateCommand();
      command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

      // The provider writes the timeout of the context into the connection string only when it creates the connection, so the current value is applied here.
      var timeout = commandTimeout is null ? _context.Database.GetCommandTimeout() : ToSeconds(commandTimeout.Value);

      if (timeout is not null)
         command.CommandTimeout = timeout.Value;

      return command;
   }

   private static int ToSeconds(TimeSpan timeout)
   {
      return (int)Math.Ceiling(timeout.TotalSeconds);
   }

   private async Task<int> InsertAsync<T>(
      IEntityType entityType,
      IEnumerable<T> entities,
      IReadOnlyList<PropertyWithNavigations> properties,
      MySqlBulkInsertOptions options,
      IBulkOperationTableNameProvider tableNameProvider,
      CancellationToken cancellationToken)
   {
      var (ownProperties, externalProperties) = properties.SeparateProperties();
      var hasExternalProperties = externalProperties.Count != 0;
      var table = tableNameProvider.GetValidTableName(entityType);
      var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema);

      using var reader = _context.GetService<IEntityDataReaderFactory>().Create(_context, entities, ownProperties, ensureReadEntitiesCollection: hasExternalProperties);
      var affectedRows = await InsertRowsAsync(reader, tableIdentifier, options.MaxParametersPerStatement, options.CommandTimeout, cancellationToken).ConfigureAwait(false);

      if (!hasExternalProperties)
         return affectedRows;

      // Owned types with their own table are inserted after their owners.
      var readEntities = (IReadOnlyList<object>)reader.GetReadEntities();

      if (readEntities.Count == 0)
         return affectedRows;

      foreach (var (navigation, ownedEntities, ownedProperties) in externalProperties.GroupExternalProperties(readEntities))
      {
         affectedRows += await InsertAsync(navigation.TargetEntityType, ownedEntities, ownedProperties, options, tableNameProvider, cancellationToken).ConfigureAwait(false);
      }

      return affectedRows;
   }

   // The entity data reader rejects an empty property list, so there is at least one column.
   private async Task<int> InsertRowsAsync(
      IEntityDataReader reader,
      string tableIdentifier,
      int? maxParametersPerStatement,
      TimeSpan? commandTimeout,
      CancellationToken cancellationToken)
   {
      var columns = GetColumnNames(reader.Properties);

      if (_options.UseBulkCopy)
         return await BulkCopyAsync(reader, tableIdentifier, columns, commandTimeout, cancellationToken).ConfigureAwait(false);

      var mappings = GetProviderMappings(reader.Properties);
      var maximumRows = GetRowsPerStatement(maxParametersPerStatement, columns.Length);
      var rows = new List<object?[]>(maximumRows);
      var affectedRows = 0;

      // The statement of a full batch is prepared once and executed for every full batch with new values.
      DbCommand? preparedCommand = null;

      try
      {
         while (reader.Read())
         {
            var row = new object?[columns.Length];

            for (var i = 0; i < row.Length; i++)
            {
               row[i] = reader.GetValue(i);
            }

            rows.Add(row);

            if (rows.Count == maximumRows)
            {
               if (preparedCommand is null)
               {
                  preparedCommand = CreateInsertCommand(tableIdentifier, columns, mappings, rows, commandTimeout);
                  await preparedCommand.PrepareAsync(cancellationToken).ConfigureAwait(false);
               }
               else
               {
                  SetParameterValues(preparedCommand, rows);
               }

               affectedRows += await preparedCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
               rows.Clear();
            }
         }
      }
      finally
      {
         if (preparedCommand is not null)
            await preparedCommand.DisposeAsync().ConfigureAwait(false);
      }

      // The last partial batch has another statement text, so it runs as a one-time statement.
      if (rows.Count != 0)
      {
         await using var command = CreateInsertCommand(tableIdentifier, columns, mappings, rows, commandTimeout);
         affectedRows += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      }

      return affectedRows;
   }

   // The data reader yields provider values, so the values of later batches are assigned without the type mapping.
   private static void SetParameterValues(DbCommand command, IReadOnlyList<object?[]> rows)
   {
      var parameterIndex = 0;

      foreach (var row in rows)
      {
         foreach (var value in row)
         {
            command.Parameters[parameterIndex++].Value = value ?? DBNull.Value;
         }
      }
   }

   private async Task<int> BulkCopyAsync(
      IEntityDataReader reader,
      string tableIdentifier,
      string[] columns,
      TimeSpan? commandTimeout,
      CancellationToken cancellationToken)
   {
      var dbConnection = _context.Database.GetDbConnection();
      var connection = dbConnection as MySqlConnection
                       ?? throw new NotSupportedException($"Bulk copy requires a '{typeof(MySqlConnection).FullName}'; the current connection is '{dbConnection.GetType().FullName}'.");

      // Checked before MySqlBulkCopy runs, because MySqlConnector keeps a failed bulk copy in a static dictionary.
      if (!new MySqlConnectionStringBuilder(connection.ConnectionString).AllowLoadLocalInfile)
         throw new NotSupportedException("Bulk operations registered with 'useBulkCopy: true' use LOAD DATA LOCAL INFILE, which requires 'AllowLoadLocalInfile=true' in the connection string.");

      // MySqlBulkCopy rejects YEAR columns only when it creates the column mappings itself; with explicit mappings it stores 2000 for the value 0.
      foreach (var property in reader.Properties)
      {
         if (MySqlTempTableCreator.GetBaseType(property.Property.GetColumnType(property.GetStoreObject())) == "year")
            throw new NotSupportedException($"The column of the property '{property.Property.Name}' has the type YEAR, which bulk operations registered with 'useBulkCopy: true' do not support.");
      }

      // The timeout follows the rule of the INSERT path: the timeout of the options, the timeout of the context, or the default of the connection.
      int timeout;

      await using (var command = CreateCommand(commandTimeout))
      {
         timeout = command.CommandTimeout;
      }

      var transaction = _context.Database.CurrentTransaction?.GetDbTransaction() as MySqlTransaction;
      var bulkCopy = new MySqlBulkCopy(connection, transaction)
                     {
                        DestinationTableName = tableIdentifier,
                        BulkCopyTimeout = timeout
                     };

      // The columns are mapped by name, because the destination may have more columns (e.g. the order column of a staging table).
      for (var i = 0; i < columns.Length; i++)
      {
         bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, columns[i]));
      }

      var source = new MySqlBulkCopyDataReader(reader);
      var result = await bulkCopy.WriteToServerAsync(source, cancellationToken).ConfigureAwait(false);

      // The copy ended at the failing row without an error of MySqlConnector. The caller gets the exception of the source, as on the INSERT path.
      source.SourceException?.Throw();

      // LOAD DATA LOCAL implies IGNORE: duplicate keys and bad values become warnings and the rows are dropped or changed.
      // A note (e.g. 1265 for a decimal that is rounded to the scale of the column) is ignored, because the INSERT path accepts the same value.
      var warnings = result.Warnings.Where(warning => !String.Equals(warning.Level, "Note", StringComparison.OrdinalIgnoreCase)).ToList();

      if (warnings.Count != 0 || result.RowsInserted != reader.RowsRead)
      {
         var details = String.Join("; ", warnings.Take(5).Select(warning => $"{warning.Level} {warning.ErrorCode}: {warning.Message}"));
         throw new InvalidOperationException($"MySqlBulkCopy inserted {result.RowsInserted} of {reader.RowsRead} rows. {details}");
      }

      return result.RowsInserted;
   }

   private DbCommand CreateInsertCommand(
      string tableIdentifier,
      IReadOnlyList<string> columnNames,
      IReadOnlyList<RelationalTypeMapping?> mappings,
      IReadOnlyList<object?[]> rows,
      TimeSpan? commandTimeout)
   {
      var command = CreateCommand(commandTimeout);

      try
      {
         var statement = new StringBuilder($"INSERT INTO {tableIdentifier} (");

         for (var columnIndex = 0; columnIndex < columnNames.Count; columnIndex++)
         {
            if (columnIndex != 0)
               statement.Append(", ");

            statement.Append(_sqlGenerationHelper.DelimitIdentifier(columnNames[columnIndex]));
         }

         statement.Append(") VALUES ");

         for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
         {
            if (rowIndex != 0)
               statement.Append(", ");

            statement.Append('(');

            for (var columnIndex = 0; columnIndex < columnNames.Count; columnIndex++)
            {
               if (columnIndex != 0)
                  statement.Append(", ");

               var parameterName = $"@p{rowIndex}_{columnIndex}";
               statement.Append(parameterName);

               var value = rows[rowIndex][columnIndex];
               DbParameter parameter;

               if (mappings[columnIndex] is { } mapping)
               {
                  parameter = mapping.CreateParameter(command, parameterName, value);
               }
               else
               {
                  parameter = command.CreateParameter();
                  parameter.ParameterName = parameterName;
                  parameter.Value = value ?? DBNull.Value;
               }

               command.Parameters.Add(parameter);
            }

            statement.Append(')');
         }

         command.CommandText = statement.ToString();
         return command;
      }
      catch
      {
         command.Dispose();
         throw;
      }
   }

   private static List<PropertyWithNavigations> ToPropertiesWithNavigations(IReadOnlyList<IProperty> properties)
   {
      return properties.Select(property => new PropertyWithNavigations(property, [])).ToList();
   }

   // EF Core executes the statement, so logging, interceptors, the concurrency detector, the current transaction and the command timeout of the context apply.
   // "Build(sql)" does not format the text, so braces in identifiers are kept.
   private Task<int> ExecuteSqlAsync(string statement, CancellationToken cancellationToken)
   {
      var command = _context.GetService<IRawSqlCommandBuilder>().Build(statement);
      return new EfCoreValueExpressionTranslator(_context).ExecuteNonQueryAsync(command, ReadOnlyDictionary<string, object?>.Empty, cancellationToken);
   }

   // Creates the staging table, fills it with the registered insert path, runs the statements of the operation and drops the staging table.
   private async Task<int> ExecuteWithStagingTableAsync<T>(
      StagingTable staging,
      IEnumerable<T> entities,
      Func<string, CancellationToken, Task<int>> executeStatementsAsync,
      CancellationToken cancellationToken)
   {
      // The statements that EF Core executes use the command timeout of the context, so the timeout of the options is applied for the operation.
      var previousCommandTimeout = _context.Database.GetCommandTimeout();

      if (staging.CommandTimeout is not null)
         _context.Database.SetCommandTimeout(ToSeconds(staging.CommandTimeout.Value));

      try
      {
         using var nameLease = ReusingTempTableNameProvider.Instance.LeaseName(_context, staging.EntityType);
         var createStatement = CreateStagingTableStatement(staging, nameLease.Name);
         int result;

         try
         {
            await ExecuteSqlAsync(createStatement, cancellationToken).ConfigureAwait(false);

            using var reader = _context.GetService<IEntityDataReaderFactory>().Create(_context, entities, staging.Properties, ensureReadEntitiesCollection: false);
            await InsertRowsAsync(reader,
                                  _sqlGenerationHelper.DelimitIdentifier(nameLease.Name),
                                  staging.MaxParametersPerStatement,
                                  staging.CommandTimeout,
                                  cancellationToken).ConfigureAwait(false);

            result = await executeStatementsAsync(nameLease.Name, cancellationToken).ConfigureAwait(false);
         }
         catch (MySqlException exception) when (exception.Number == _XA_TEMPORARY_TABLE_ERROR)
         {
            // MySQL rejects every access to a temporary table inside XA, so there is no table to drop.
            throw new NotSupportedException("Bulk update and upsert cannot run inside an XA transaction, because MySQL rejects temporary tables there. Use 'UseXaTransactions=false' in the connection string, a DbContext transaction, or the server setting 'xa_detach_on_prepare=OFF'.", exception);
         }
         catch (MySqlException exception) when (exception.Number == _TABLE_EXISTS_ERROR)
         {
            // The existing table with the leased name does not belong to the operation, so it is not dropped.
            throw;
         }
         catch
         {
            // The server may have created the table although the CREATE failed on the client (cancellation, timeout), so the drop runs after every error.
            try
            {
               await DropStagingTableAsync(nameLease.Name).ConfigureAwait(false);
            }
            catch (Exception)
            {
               // The original exception is more relevant than a failed cleanup. The temporary table disappears with the session at the latest.
            }

            throw;
         }

         await DropStagingTableAsync(nameLease.Name).ConfigureAwait(false);
         return result;
      }
      finally
      {
         _context.Database.SetCommandTimeout(previousCommandTimeout);
      }
   }

   // The cleanup runs also after a cancellation, so it does not use the token of the operation.
   private Task<int> DropStagingTableAsync(string stagingTable)
   {
      return ExecuteSqlAsync($"DROP TEMPORARY TABLE IF EXISTS {_sqlGenerationHelper.DelimitIdentifier(stagingTable)};", CancellationToken.None);
   }

   // The SELECT copies type, charset, collation and NOT NULL of the target columns, so values are converted and keys are compared as in the target.
   // "__ord" numbers the rows in input order and is the primary key, which "sql_require_primary_key" demands also for temporary tables.
   private string CreateStagingTableStatement(StagingTable staging, string stagingTable)
   {
      var statement = new StringBuilder("CREATE TEMPORARY TABLE ");
      statement.Append(_sqlGenerationHelper.DelimitIdentifier(stagingTable))
               .Append(" (")
               .Append(_sqlGenerationHelper.DelimitIdentifier(_ORDER_COLUMN))
               .Append(" BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY");

      // The unique index rejects duplicate input keys (error 1062).
      if (HasStagingKeyIndex(staging))
      {
         statement.Append(", UNIQUE (")
                  .AppendJoin(", ", GetColumnNames(staging.Keys).Select(column => _sqlGenerationHelper.DelimitIdentifier(column)))
                  .Append(')');
      }

      statement.Append(") SELECT ")
               .AppendJoin(", ", GetColumnNames(staging.Properties).Select(column => _sqlGenerationHelper.DelimitIdentifier(column)))
               .Append(" FROM ")
               .Append(_sqlGenerationHelper.DelimitIdentifier(staging.Target.Name, staging.Target.Schema))
               .Append(" LIMIT 0;");

      return statement.ToString();
   }

   private static bool HasStagingKeyIndex(StagingTable staging)
   {
      return staging.Keys.All(key =>
                              {
                                 var storeObject = key.GetStoreObject();

                                 // Several new rows of an upsert may have the key 0 of an AUTO_INCREMENT column; the INSERT generates their ids.
                                 if (staging.IsUpsert
                                     && key.Property.GetValueGenerationStrategy(storeObject) == MySqlValueGenerationStrategy.IdentityColumn
                                     && MySqlPropertyExtensions.IsCompatibleAutoIncrementColumn(key.Property))
                                    return false;

                                 // MySQL cannot index a TEXT, BLOB or JSON column without a prefix length (error 1170).
                                 return !MySqlTempTableCreator.IsTextBlobOrJson(key.Property.GetColumnType(storeObject));
                              });
   }

   private async Task<int> UpsertFromStagingTableAsync(
      StagingTable staging,
      string stagingTable,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> propertiesToSet,
      CancellationToken cancellationToken)
   {
      var updatedRows = propertiesToSet.Count == 0
                           ? 0
                           : await ExecuteSqlAsync(CreateUpdateStatement(staging.Target, stagingTable, propertiesToSet, staging.Keys), cancellationToken).ConfigureAwait(false);

      // The rows that match a target row are removed, so the remaining rows are the new ones.
      var deleteStatement = new StringBuilder("DELETE ").Append(_SAFE_UPDATES_OFF_HINT).Append(" s FROM ");
      deleteStatement.Append(_sqlGenerationHelper.DelimitIdentifier(stagingTable))
                     .Append(" AS s INNER JOIN ")
                     .Append(_sqlGenerationHelper.DelimitIdentifier(staging.Target.Name, staging.Target.Schema))
                     .Append(" AS d ON ");
      AppendKeyCondition(deleteStatement, staging.Keys).Append(';');
      await ExecuteSqlAsync(deleteStatement.ToString(), cancellationToken).ConfigureAwait(false);

      // InnoDB checks foreign keys row by row, so the rows are inserted in input order (a parent before its child).
      var columns = GetColumnNames(propertiesToInsert).Select(column => _sqlGenerationHelper.DelimitIdentifier(column)).ToList();
      var insertStatement = new StringBuilder("INSERT INTO ");
      insertStatement.Append(_sqlGenerationHelper.DelimitIdentifier(staging.Target.Name, staging.Target.Schema))
                     .Append(" (")
                     .AppendJoin(", ", columns)
                     .Append(") SELECT ")
                     .AppendJoin(", ", columns.Select(column => $"s.{column}"))
                     .Append(" FROM ")
                     .Append(_sqlGenerationHelper.DelimitIdentifier(stagingTable))
                     .Append(" AS s ORDER BY s.")
                     .Append(_sqlGenerationHelper.DelimitIdentifier(_ORDER_COLUMN))
                     .Append(';');
      var insertedRows = await ExecuteSqlAsync(insertStatement.ToString(), cancellationToken).ConfigureAwait(false);

      return updatedRows + insertedRows;
   }

   private string CreateUpdateStatement(
      BulkOperationTableName table,
      string stagingTable,
      IReadOnlyList<PropertyWithNavigations> propertiesToSet,
      IReadOnlyList<PropertyWithNavigations> keys)
   {
      var statement = new StringBuilder("UPDATE ").Append(_SAFE_UPDATES_OFF_HINT).Append(' ');

      statement.Append(_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema))
               .Append(" AS d INNER JOIN ")
               .Append(_sqlGenerationHelper.DelimitIdentifier(stagingTable))
               .Append(" AS s ON ");
      AppendKeyCondition(statement, keys).Append(" SET ");

      var setColumns = GetColumnNames(propertiesToSet);

      for (var i = 0; i < setColumns.Length; i++)
      {
         if (i != 0)
            statement.Append(", ");

         var column = _sqlGenerationHelper.DelimitIdentifier(setColumns[i]);
         statement.Append("d.").Append(column).Append(" = s.").Append(column);
      }

      statement.Append(';');
      return statement.ToString();
   }

   // "<=>" matches NULL with NULL for nullable keys.
   private StringBuilder AppendKeyCondition(StringBuilder statement, IReadOnlyList<PropertyWithNavigations> keys)
   {
      var keyColumns = GetColumnNames(keys);

      for (var i = 0; i < keyColumns.Length; i++)
      {
         if (i != 0)
            statement.Append(" AND ");

         var column = _sqlGenerationHelper.DelimitIdentifier(keyColumns[i]);
         statement.Append("d.").Append(column).Append(keys[i].Property.IsNullable ? " <=> s." : " = s.").Append(column);
      }

      return statement;
   }

   /// <summary>
   /// Describes the staging table of a bulk update or upsert.
   /// </summary>
   /// <param name="EntityType">Entity type whose rows are staged.</param>
   /// <param name="Target">Resolved table that is updated.</param>
   /// <param name="Keys">Key properties to match on.</param>
   /// <param name="Properties">Staged properties; the keys come first.</param>
   /// <param name="MaxParametersPerStatement">Parameter budget of the INSERT statements of the fill; <c>null</c> uses the registered value.</param>
   /// <param name="CommandTimeout">Timeout of every statement.</param>
   /// <param name="IsUpsert">Indication whether the operation is an upsert; an upsert stages a store-generated key without a key index.</param>
   private sealed record StagingTable(
      IEntityType EntityType,
      BulkOperationTableName Target,
      IReadOnlyList<PropertyWithNavigations> Keys,
      IReadOnlyList<PropertyWithNavigations> Properties,
      int? MaxParametersPerStatement,
      TimeSpan? CommandTimeout,
      bool IsUpsert);
}
