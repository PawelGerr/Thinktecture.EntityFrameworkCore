using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using System.Transactions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Executes MySQL bulk inserts.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class MySqlBulkOperationExecutor : IBulkInsertExecutor, ITempTableBulkInsertExecutor, IBulkUpdateExecutor, IBulkInsertOrUpdateExecutor, ITruncateTableExecutor
{
   private const int _MAX_PARAMETERS = 65_535;
   private const int _MAX_ROWS_PER_BATCH = 500;

   private readonly DbContext _context;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly bool _useRowAlias;

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlBulkOperationExecutor" />.
   /// </summary>
   /// <param name="context">Current database context.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mapping source used to create command parameters.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   public MySqlBulkOperationExecutor(
      ICurrentDbContext context,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource,
      IMySqlOptions mySqlOptions)
   {
      ArgumentNullException.ThrowIfNull(context);
      ArgumentNullException.ThrowIfNull(mySqlOptions);
      _context = context.Context ?? throw new ArgumentNullException(nameof(context));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));

      // VALUES(col) is deprecated since MySQL 8.0.20; the row alias syntax exists since 8.0.19 and is unsupported by MariaDB.
      _useRowAlias = mySqlOptions.ServerVersion.Type == ServerType.MySql && mySqlOptions.ServerVersion.Version >= new Version(8, 0, 19);
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

      return await ExecuteInTransactionAsync(token => InsertAsync(entityType, entities, properties, mySqlOptions, token), cancellationToken).ConfigureAwait(false);
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
      var table = mySqlOptions.TableNameProvider.GetValidTableName(entityType);
      var propertiesToUpdate = mySqlOptions.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, null);
      var keyProperties = mySqlOptions.KeyProperties.DetermineKeyProperties(entityType);

      if (keyProperties.Count == 0)
         throw new InvalidOperationException($"The entity type '{entityType.Name}' needs a primary key to perform bulk update.");

      propertiesToUpdate.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();
      EnsureNoSeparateOwnedTypes(entityType);

      var propertiesToSet = propertiesToUpdate.Where(property => !keyProperties.Contains(property.Property)).ToList();

      if (propertiesToSet.Count == 0)
         throw new ArgumentException("The number of properties to update cannot be 0.");

      var tempTableOptions = new MySqlTempTableBulkInsertOptions
                             {
                                PropertiesToInsert = mySqlOptions.PropertiesToUpdate is null
                                                        ? null
                                                        : CompositeTempTableEntityPropertiesProvider.CreateForUpdate(mySqlOptions.PropertiesToUpdate, mySqlOptions.KeyProperties),
                                BatchSize = mySqlOptions.BatchSize,
                                CommandTimeout = mySqlOptions.CommandTimeout,
                                // The update keys may not be the primary key.
                                PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration
                             };

      return await ExecuteInTransactionAsync(async token =>
                                             {
                                                // MySQL rejects temp tables inside the XA transaction of a TransactionScope.
                                                if (Transaction.Current is not null)
                                                   return await UpdateRowByRowAsync(entities, table, propertiesToSet, keyProperties, mySqlOptions.CommandTimeout, token).ConfigureAwait(false);

                                                // Staging in a temp table turns N round trips into one set-based UPDATE.
                                                await using var tempTable = await BulkInsertIntoTempTableAsync<T, T>(entities, tempTableOptions, static query => query, token).ConfigureAwait(false);
                                                var statement = CreateUpdateStatement(table, tempTable.Name, propertiesToSet, keyProperties);

                                                await using var command = CreateCommand(mySqlOptions.CommandTimeout);
                                                command.CommandText = statement;
                                                return await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                                             },
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
      var table = mySqlOptions.TableNameProvider.GetValidTableName(entityType);
      var propertiesToInsert = mySqlOptions.PropertiesToInsert.DeterminePropertiesForInsert(entityType, null);
      var propertiesToUpdate = mySqlOptions.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, true);
      var keyProperties = mySqlOptions.KeyProperties.DetermineKeyProperties(entityType);

      propertiesToInsert.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();
      propertiesToUpdate.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();
      EnsureNoSeparateOwnedTypes(entityType);

      // Without an explicit selection the inserted properties are updated. The update can only use the values that are inserted
      // (the incoming row), so properties that are explicitly selected for update must be inserted as well (checked below).
      if (mySqlOptions.PropertiesToUpdate is null)
         propertiesToUpdate = propertiesToUpdate.Where(propertiesToInsert.Contains).ToList();

      foreach (var property in propertiesToUpdate)
      {
         if (!propertiesToInsert.Contains(property))
            throw new NotSupportedException($"MySQL upsert requires update property '{property.Property.Name}' to also be selected for insertion.");
      }

      foreach (var keyProperty in keyProperties)
      {
         if (!propertiesToInsert.Any(property => property.Property == keyProperty))
            throw new NotSupportedException($"MySQL upsert requires key property '{keyProperty.Name}' to be selected for insertion.");
      }

      var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema);
      var insertColumns = GetColumnNames(propertiesToInsert);
      var mappings = GetProviderMappings(propertiesToInsert);
      // The primary key is never rewritten by default, even if other properties are used as the upsert key.
      IReadOnlyList<IProperty> primaryKeyProperties = mySqlOptions.PropertiesToUpdate is null
                                                         ? entityType.FindPrimaryKey()?.Properties ?? []
                                                         : [];
      var updateColumns = GetColumnNames(propertiesToUpdate.Where(property => !keyProperties.Contains(property.Property) && !primaryKeyProperties.Contains(property.Property)).ToList());
      var keyColumns = GetColumnNames(keyProperties.Select(property => new PropertyWithNavigations(property, Array.Empty<Navigation>())).ToList());
      var maximumRows = GetMaximumRows(mySqlOptions.BatchSize, propertiesToInsert.Count);

      return await ExecuteInTransactionAsync(async token =>
                                             {
                                                using var reader = _context.GetService<IEntityDataReaderFactory>().Create(_context, entities, propertiesToInsert, ensureReadEntitiesCollection: false);
                                                return await InsertRowsAsync(reader,
                                                                             tableIdentifier,
                                                                             insertColumns,
                                                                             mappings,
                                                                             maximumRows,
                                                                             mySqlOptions.CommandTimeout,
                                                                             updateColumns,
                                                                             keyColumns,
                                                                             false,
                                                                             token).ConfigureAwait(false);
                                             },
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

      var logger = _context.GetService<ILoggerFactory>().CreateLogger<MySqlBulkOperationExecutor>();
      var translator = new EfCoreValueExpressionTranslator(logger, _context);
      var (sql, parameters) = translator.TranslateUpdateFromQuery(sourceQuery,
                                                                  targetKeySelector,
                                                                  sourceKeySelector,
                                                                  setClauseBuilder.Entries,
                                                                  filter,
                                                                  options?.TableNameProvider ?? IBulkOperationTableNameProvider.Default);
      var sqlParameters = parameters.Select(parameter => (object)new MySqlParameter(parameter.Key, parameter.Value ?? DBNull.Value));
      return await _context.Database.ExecuteSqlRawAsync(EscapeFormatString(sql), sqlParameters, cancellationToken).ConfigureAwait(false);
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

      var logger = _context.GetService<ILoggerFactory>().CreateLogger<MySqlBulkOperationExecutor>();
      var translator = new EfCoreValueExpressionTranslator(logger, _context);
      var (sql, parameters) = translator.TranslateInsertFromQuery<TTarget, TSource>(
                                                                                    sourceQuery,
                                                                                    builder.Entries,
                                                                                    options?.TableNameProvider ?? IBulkOperationTableNameProvider.Default);
      var sqlParameters = parameters.Select(parameter => (object)new MySqlParameter(parameter.Key, parameter.Value ?? DBNull.Value));
      return await _context.Database.ExecuteSqlRawAsync(EscapeFormatString(sql), sqlParameters, cancellationToken).ConfigureAwait(false);
   }

   /// <inheritdoc />
   public Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
      where T : class
   {
      return TruncateTableAsync(typeof(T), cancellationToken);
   }

   /// <inheritdoc />
   /// <remarks>
   /// Uses <c>TRUNCATE TABLE</c>, or <c>DELETE FROM</c> if a transaction is active (TRUNCATE commits implicitly)
   /// or the table is referenced by a foreign key (TRUNCATE fails with error 1701).
   /// <c>DELETE</c> does not reset <c>AUTO_INCREMENT</c> counters.
   /// </remarks>
   public async Task TruncateTableAsync(Type type, CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(type);

      var entityType = _context.Model.GetEntityType(type);
      var tableName = entityType.GetTableName()
                      ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var delimitedTable = _sqlGenerationHelper.DelimitIdentifier(tableName, entityType.GetSchema());

      // TRUNCATE commits implicitly and fails (1701) on tables referenced by foreign keys, even if the child is empty.
      var useDelete = HasTransaction
                      || entityType.GetReferencingForeignKeys().Any();
      var sql = useDelete ? $"DELETE FROM {delimitedTable};" : $"TRUNCATE TABLE {delimitedTable};";

      await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
   }

   // ExecuteSqlRaw passes the SQL through string.Format. Braces of the generated SQL (for example in string literals) must not be treated as placeholders.
   private static string EscapeFormatString(string sql)
   {
      return sql.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
   }

   private static void EnsureNoSeparateOwnedTypes(IEntityType entityType)
   {
      if (HasSeparateOwnedType(entityType, entityType.GetTableName(), entityType.GetSchema()))
         throw new NotSupportedException("MySQL bulk update and upsert do not support owned entities mapped to separate tables.");
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

   private static int GetMaximumRows(int? batchSize, int numberOfColumns)
   {
      if (batchSize is <= 0)
         throw new ArgumentOutOfRangeException(nameof(batchSize), "The batch size must be greater than 0.");

      return Math.Max(1, Math.Min(batchSize ?? _MAX_ROWS_PER_BATCH, _MAX_PARAMETERS / Math.Max(1, numberOfColumns)));
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

   private bool HasTransaction => _context.Database.CurrentTransaction is not null || Transaction.Current is not null;

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

      if (commandTimeout is not null)
         command.CommandTimeout = (int)Math.Ceiling(commandTimeout.Value.TotalSeconds);

      return command;
   }

   private async Task<int> InsertAsync<T>(
      IEntityType entityType,
      IEnumerable<T> entities,
      IReadOnlyList<PropertyWithNavigations> properties,
      MySqlBulkInsertOptions options,
      CancellationToken cancellationToken)
   {
      var (ownProperties, externalProperties) = properties.SeparateProperties();
      var hasExternalProperties = externalProperties.Count != 0;
      var table = options.TableNameProvider.GetValidTableName(entityType);
      var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema);
      var maximumRows = GetMaximumRows(options.BatchSize, ownProperties.Count);

      using var reader = _context.GetService<IEntityDataReaderFactory>().Create(_context, entities, ownProperties, ensureReadEntitiesCollection: hasExternalProperties);
      var affectedRows = await InsertRowsAsync(reader,
                                               tableIdentifier,
                                               GetColumnNames(ownProperties),
                                               GetProviderMappings(ownProperties),
                                               maximumRows,
                                               options.CommandTimeout,
                                               null,
                                               null,
                                               options.UseBulkCopy,
                                               cancellationToken).ConfigureAwait(false);

      if (!hasExternalProperties)
         return affectedRows;

      // Owned types with their own table are inserted after their owners.
      var readEntities = (IReadOnlyList<object>)reader.GetReadEntities();

      if (readEntities.Count == 0)
         return affectedRows;

      foreach (var (navigation, ownedEntities, ownedProperties) in externalProperties.GroupExternalProperties(readEntities))
      {
         affectedRows += await InsertAsync(navigation.TargetEntityType, ownedEntities, ownedProperties, options, cancellationToken).ConfigureAwait(false);
      }

      return affectedRows;
   }

   private async Task<int> InsertRowsAsync(
      IEntityDataReader reader,
      string tableIdentifier,
      IReadOnlyList<string> columns,
      IReadOnlyList<RelationalTypeMapping?> mappings,
      int maximumRows,
      TimeSpan? commandTimeout,
      IReadOnlyList<string>? updateColumns,
      IReadOnlyList<string>? keyColumns,
      bool useBulkCopy,
      CancellationToken cancellationToken)
   {
      if (useBulkCopy)
      {
         var connection = (MySqlConnection)_context.Database.GetDbConnection();
         var transaction = _context.Database.CurrentTransaction?.GetDbTransaction() as MySqlTransaction;
         var data = new System.Data.DataTable();

         for (var i = 0; i < columns.Count; i++)
         {
            var clrType = mappings[i]?.ClrType ?? typeof(object);
            data.Columns.Add(columns[i], Nullable.GetUnderlyingType(clrType) ?? clrType);
         }

         while (reader.Read())
         {
            var row = data.NewRow();

            for (var i = 0; i < columns.Count; i++)
            {
               row[i] = reader.GetValue(i) ?? DBNull.Value;
            }

            data.Rows.Add(row);
         }

         var bulkCopy = new MySqlBulkCopy(connection, transaction)
                        {
                           DestinationTableName = tableIdentifier,
                           // 0 would mean "wait forever".
                           BulkCopyTimeout = commandTimeout is null
                                                ? _context.Database.GetCommandTimeout() ?? 30
                                                : (int)Math.Ceiling(commandTimeout.Value.TotalSeconds)
                        };

         for (var i = 0; i < columns.Count; i++)
         {
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, columns[i]));
         }

         var result = await bulkCopy.WriteToServerAsync(data, cancellationToken).ConfigureAwait(false);

         // LOAD DATA LOCAL implies IGNORE: duplicate keys and bad values become warnings and the rows are dropped.
         if (result.Warnings.Count != 0 || result.RowsInserted != data.Rows.Count)
         {
            var details = String.Join("; ", result.Warnings.Take(5).Select(warning => $"{warning.Level} {warning.ErrorCode}: {warning.Message}"));
            throw new InvalidOperationException($"MySqlBulkCopy inserted {result.RowsInserted} of {data.Rows.Count} rows. {details}");
         }

         return result.RowsInserted;
      }

      var rows = new List<object?[]>(maximumRows);
      var affectedRows = 0;

      while (reader.Read())
      {
         var row = new object?[columns.Count];

         for (var i = 0; i < row.Length; i++)
         {
            row[i] = reader.GetValue(i);
         }

         rows.Add(row);

         if (rows.Count == maximumRows)
         {
            affectedRows += await ExecuteAndCountAsync().ConfigureAwait(false);
            rows.Clear();
         }
      }

      if (rows.Count != 0)
         affectedRows += await ExecuteAndCountAsync().ConfigureAwait(false);

      return affectedRows;

      // An upsert reports 1/2/0 per row depending on insert/update/no-change; report the processed rows instead.
      async Task<int> ExecuteAndCountAsync()
      {
         var affected = await ExecuteBatchAsync(tableIdentifier, columns, mappings, rows, commandTimeout, cancellationToken, updateColumns, keyColumns).ConfigureAwait(false);
         return updateColumns is null ? affected : rows.Count;
      }
   }

   private async Task<int> ExecuteBatchAsync(
      string tableIdentifier,
      IReadOnlyList<string> columnNames,
      IReadOnlyList<RelationalTypeMapping?> mappings,
      IReadOnlyList<object?[]> rows,
      TimeSpan? commandTimeout,
      CancellationToken cancellationToken,
      IReadOnlyList<string>? updateColumnNames = null,
      IReadOnlyList<string>? keyColumnNames = null)
   {
      await using var command = CreateCommand(commandTimeout);

      var statement = new StringBuilder($"INSERT INTO {tableIdentifier} ");

      if (columnNames.Count == 0)
      {
         statement.Append("() VALUES ");

         for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
         {
            if (rowIndex != 0)
               statement.Append(", ");

            statement.Append("()");
         }
      }
      else
      {
         statement.Append('(');

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
      }

      if (updateColumnNames is not null)
      {
         if (_useRowAlias)
            statement.Append(" AS `new_row`");

         statement.Append(" ON DUPLICATE KEY UPDATE ");

         if (keyColumnNames is not { Count: > 0 })
            throw new InvalidOperationException("At least one key column is required for MySQL upsert.");

         if (updateColumnNames.Count == 0)
         {
            var delimitedKeyColumn = _sqlGenerationHelper.DelimitIdentifier(keyColumnNames[0]);
            statement.Append(delimitedKeyColumn).Append(" = ").Append(Existing(delimitedKeyColumn));
         }
         else
         {
            // MySQL fires ON DUPLICATE KEY for any unique index; only rows matching the keys are updated.
            var keyMatch = String.Join(" AND ", keyColumnNames.Select(key =>
                                                                      {
                                                                         var column = _sqlGenerationHelper.DelimitIdentifier(key);
                                                                         return $"{Existing(column)} <=> {Incoming(column)}";
                                                                      }));

            for (var i = 0; i < updateColumnNames.Count; i++)
            {
               if (i != 0)
                  statement.Append(", ");

               var column = _sqlGenerationHelper.DelimitIdentifier(updateColumnNames[i]);
               statement.Append(column).Append(" = IF(").Append(keyMatch).Append(", ").Append(Incoming(column)).Append(", ").Append(Existing(column)).Append(')');
            }
         }
      }

      command.CommandText = statement.ToString();
      return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

      string Existing(string column) => _useRowAlias ? $"{tableIdentifier}.{column}" : column;
      string Incoming(string column) => _useRowAlias ? $"`new_row`.{column}" : $"VALUES({column})";
   }

   private async Task<int> UpdateRowByRowAsync<T>(
      IEnumerable<T> entities,
      BulkOperationTableName table,
      IReadOnlyCollection<PropertyWithNavigations> propertiesToSet,
      IReadOnlyList<IProperty> keyProperties,
      TimeSpan? commandTimeout,
      CancellationToken cancellationToken)
   {
      var readerProperties = propertiesToSet.Concat(keyProperties.Select(property => new PropertyWithNavigations(property, Array.Empty<Navigation>()))).ToList();
      var columns = GetColumnNames(readerProperties);
      var mappings = GetProviderMappings(readerProperties);
      var statement = new StringBuilder($"UPDATE {_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema)} SET ");

      for (var i = 0; i < propertiesToSet.Count; i++)
      {
         statement.Append(i == 0 ? "" : ", ").Append(_sqlGenerationHelper.DelimitIdentifier(columns[i])).Append(" = @p").Append(i);
      }

      statement.Append(" WHERE ");

      for (var i = propertiesToSet.Count; i < columns.Length; i++)
      {
         statement.Append(i == propertiesToSet.Count ? "" : " AND ")
                  .Append(_sqlGenerationHelper.DelimitIdentifier(columns[i]))
                  .Append(keyProperties[i - propertiesToSet.Count].IsNullable ? " <=> @p" : " = @p")
                  .Append(i);
      }

      await using var command = CreateCommand(commandTimeout);
      command.CommandText = statement.ToString();

      var parameters = new DbParameter[columns.Length];

      for (var i = 0; i < parameters.Length; i++)
      {
         parameters[i] = mappings[i]?.CreateParameter(command, $"@p{i}", null) ?? command.CreateParameter();
         parameters[i].ParameterName = $"@p{i}";
         command.Parameters.Add(parameters[i]);
      }

      using var reader = _context.GetService<IEntityDataReaderFactory>().Create(_context, entities, readerProperties, ensureReadEntitiesCollection: false);
      var affectedRows = 0;

      while (reader.Read())
      {
         for (var i = 0; i < parameters.Length; i++)
         {
            parameters[i].Value = reader.GetValue(i) ?? DBNull.Value;
         }

         affectedRows += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      }

      return affectedRows;
   }

   private string CreateUpdateStatement(
      BulkOperationTableName table,
      string sourceTempTableName,
      IReadOnlyList<PropertyWithNavigations> propertiesToSet,
      IReadOnlyList<IProperty> keyProperties)
   {
      var setColumns = GetColumnNames(propertiesToSet);
      var keyColumns = GetColumnNames(keyProperties.Select(property => new PropertyWithNavigations(property, Array.Empty<Navigation>())).ToList());
      var statement = new StringBuilder("UPDATE ");

      statement.Append(_sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema))
               .Append(" AS d INNER JOIN ")
               .Append(_sqlGenerationHelper.DelimitIdentifier(sourceTempTableName))
               .Append(" AS s ON ");

      for (var i = 0; i < keyColumns.Length; i++)
      {
         if (i != 0)
            statement.Append(" AND ");

         var column = _sqlGenerationHelper.DelimitIdentifier(keyColumns[i]);
         statement.Append("d.").Append(column).Append(keyProperties[i].IsNullable ? " <=> s." : " = s.").Append(column);
      }

      statement.Append(" SET ");

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
}
