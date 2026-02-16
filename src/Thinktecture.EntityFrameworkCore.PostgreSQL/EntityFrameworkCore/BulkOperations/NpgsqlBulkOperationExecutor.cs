using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Executes bulk operations for PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class NpgsqlBulkOperationExecutor
   : IBulkInsertExecutor, ITempTableBulkInsertExecutor, IBulkUpdateExecutor,
     IBulkInsertOrUpdateExecutor, INpgsqlTruncateTableExecutor
{
   private readonly DbContext _ctx;
   private readonly IDiagnosticsLogger<NpgsqlDbLoggerCategory.BulkOperation> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly ObjectPool<StringBuilder> _stringBuilderPool;

   private static class EventIds
   {
      public static readonly EventId Inserting = 0;
      public static readonly EventId Inserted = 1;
   }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlBulkOperationExecutor"/>.
   /// </summary>
   /// <param name="ctx">Current database context.</param>
   /// <param name="logger">Logger.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="stringBuilderPool">String builder pool.</param>
   public NpgsqlBulkOperationExecutor(
      ICurrentDbContext ctx,
      IDiagnosticsLogger<NpgsqlDbLoggerCategory.BulkOperation> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      ObjectPool<StringBuilder> stringBuilderPool)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      _ctx = ctx.Context;
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
   }

   /// <inheritdoc />
   IBulkInsertOptions IBulkInsertExecutor.CreateOptions(IEntityPropertiesProvider? propertiesToInsert)
   {
      return new NpgsqlBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   public IBulkInsertOptions CreateBulkInsertOptions(IEntityPropertiesProvider? propertiesToInsert = null)
   {
      return new NpgsqlBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   ITempTableBulkInsertOptions ITempTableBulkInsertExecutor.CreateOptions(IEntityPropertiesProvider? propertiesToInsert)
   {
      return new NpgsqlTempTableBulkInsertOptions { PropertiesToInsert = propertiesToInsert };
   }

   /// <inheritdoc />
   IBulkUpdateOptions IBulkUpdateExecutor.CreateOptions(IEntityPropertiesProvider? propertiesToUpdate, IEntityPropertiesProvider? keyProperties)
   {
      return new NpgsqlBulkUpdateOptions
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
      return new NpgsqlBulkInsertOrUpdateOptions
             {
                PropertiesToInsert = propertiesToInsert,
                PropertiesToUpdate = propertiesToUpdate,
                KeyProperties = keyProperties
             };
   }

   /// <inheritdoc />
   public Task<int> BulkInsertAsync<T>(
      IEnumerable<T> entities,
      IBulkInsertOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var entityType = _ctx.Model.GetEntityType(typeof(T));
      var tableName = options.TableName ?? entityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var schema = options.Schema ?? entityType.GetSchema();

      return BulkInsertAsync(entityType, entities, schema, tableName, options, NpgsqlBulkOperationContextFactoryForEntities.Instance, cancellationToken);
   }

   private async Task<int> BulkInsertAsync<T>(
      IEntityType entityType,
      IEnumerable<T> entitiesOrValues,
      string? schema,
      string tableName,
      IBulkInsertOptions options,
      INpgsqlBulkOperationContextFactory bulkOperationContextFactory,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(entitiesOrValues);
      ArgumentNullException.ThrowIfNull(tableName);
      ArgumentNullException.ThrowIfNull(options);

      if (options is not NpgsqlBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlBulkInsertOptions(options);

      var properties = npgsqlOptions.PropertiesToInsert.DeterminePropertiesForInsert(entityType, null);
      properties.EnsureNoSeparateOwnedTypesInsideCollectionOwnedType();

      var ctx = bulkOperationContextFactory.CreateForBulkInsert(_ctx, npgsqlOptions, properties);

      return await BulkInsertAsync(entitiesOrValues, schema, tableName, ctx, npgsqlOptions.Freeze, cancellationToken);
   }

   private async Task<int> BulkInsertAsync<T>(
      IEnumerable<T> entitiesOrValues,
      string? schema,
      string tableName,
      INpgsqlBulkOperationContext ctx,
      bool freeze,
      CancellationToken cancellationToken)
   {
      using var reader = ctx.CreateReader(entitiesOrValues);

      var (copySql, columns, npgsqlDbTypes) = BuildCopySql(reader, schema, tableName, freeze);

      IReadOnlyList<object>? readEntities = null;
      int numberOfInsertedRows;

      await _ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

      try
      {
         var connection = ctx.Connection;

         LogInserting(copySql, columns);
         var stopwatch = Stopwatch.StartNew();

         await using (var importer = await connection.BeginBinaryImportAsync(copySql, cancellationToken).ConfigureAwait(false))
         {
            while (reader.Read())
            {
               await importer.StartRowAsync(cancellationToken).ConfigureAwait(false);

               for (var i = 0; i < reader.Properties.Count; i++)
               {
                  var value = reader.GetValue(i);

                  if (value is null)
                  {
                     await importer.WriteNullAsync(cancellationToken).ConfigureAwait(false);
                  }
                  else if (npgsqlDbTypes[i] is { } dbType)
                  {
                     await importer.WriteAsync(value, dbType, cancellationToken).ConfigureAwait(false);
                  }
                  else
                  {
                     await importer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                  }
               }
            }

            numberOfInsertedRows = (int)await importer.CompleteAsync(cancellationToken).ConfigureAwait(false);
         }

         LogInserted(stopwatch.Elapsed, copySql, columns);

         if (ctx.HasExternalProperties)
         {
            readEntities = (IReadOnlyList<object>)reader.GetReadEntities();
         }
      }
      finally
      {
         await _ctx.Database.CloseConnectionAsync().ConfigureAwait(false);
      }

      if (readEntities is { Count: > 0 })
         numberOfInsertedRows += await BulkInsertSeparatedOwnedEntitiesAsync(readEntities, ctx, cancellationToken);

      return numberOfInsertedRows;
   }

   private async Task<int> BulkInsertSeparatedOwnedEntitiesAsync(
      IReadOnlyList<object> parentEntities,
      INpgsqlBulkOperationContext parentBulkOperationContext,
      CancellationToken cancellationToken)
   {
      if (parentEntities.Count == 0)
         return 0;

      var numberOfInsertedRows = 0;

      foreach (var childContext in parentBulkOperationContext.GetChildren(parentEntities))
      {
         var childTableName = childContext.EntityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{childContext.EntityType.Name}' has no table name.");

         numberOfInsertedRows += await BulkInsertAsync(childContext.Entities,
                                                       childContext.EntityType.GetSchema(),
                                                       childTableName,
                                                       childContext,
                                                       freeze: false,
                                                       cancellationToken).ConfigureAwait(false);
      }

      return numberOfInsertedRows;
   }

   private (string CopySql, string Columns, NpgsqlDbType?[] NpgsqlDbTypes) BuildCopySql(IEntityDataReader reader, string? schema, string tableName, bool freeze)
   {
      var sb = _stringBuilderPool.Get();
      var columnsSb = _stringBuilderPool.Get();
      var npgsqlDbTypes = new NpgsqlDbType?[reader.Properties.Count];

      try
      {
         sb.Append("COPY ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(tableName, schema))
           .Append(" (");

         StoreObjectIdentifier? storeObject = null;

         for (var i = 0; i < reader.Properties.Count; i++)
         {
            var property = reader.Properties[i];

            storeObject ??= property.GetStoreObject();
            var columnName = property.GetColumnName(storeObject.Value);
            var columnType = property.Property.GetColumnType(storeObject.Value);

            npgsqlDbTypes[i] = property.Property.GetTypeMapping() is INpgsqlTypeMapping npgsqlTypeMapping
                                  ? npgsqlTypeMapping.NpgsqlDbType
                                  : null;

            if (i > 0)
            {
               sb.Append(", ");
               columnsSb.Append(", ");
            }

            sb.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
            columnsSb.Append(columnName).Append(' ').Append(columnType);
         }

         sb.Append(freeze ? ") FROM STDIN (FORMAT BINARY, FREEZE)" : ") FROM STDIN (FORMAT BINARY)");

         return (sb.ToString(), columnsSb.ToString(), npgsqlDbTypes);
      }
      finally
      {
         _stringBuilderPool.Return(sb);
         _stringBuilderPool.Return(columnsSb);
      }
   }

   private void LogInserting(string copySql, string columns)
   {
      _logger.Logger.LogDebug(EventIds.Inserting,
                              """
                              Executing DbCommand
                              {CopySql}
                              Columns: {Columns}
                              """,
                              copySql,
                              columns);
   }

   private void LogInserted(TimeSpan duration, string copySql, string columns)
   {
      _logger.Logger.LogInformation(EventIds.Inserted,
                                    """
                                    Executed DbCommand ({Duration}ms)
                                    {CopySql}
                                    Columns: {Columns}
                                    """,
                                    (long)duration.TotalMilliseconds,
                                    copySql,
                                    columns);
   }

   /// <inheritdoc />
   public Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
      IEnumerable<TColumn1> values,
      ITempTableBulkInsertOptions options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(values);
      ArgumentNullException.ThrowIfNull(options);

      if (options is not NpgsqlTempTableBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlTempTableBulkInsertOptions(options);

      return BulkInsertIntoTempTableAsync<TColumn1, TempTable<TColumn1>>(values,
                                                                         npgsqlOptions,
                                                                         NpgsqlBulkOperationContextFactoryForValues.Instance,
                                                                         query => query.Select(t => t.Column1),
                                                                         cancellationToken);
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

      if (options is not NpgsqlTempTableBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlTempTableBulkInsertOptions(options);

      return BulkInsertIntoTempTableAsync<T, T>(entities,
                                                npgsqlOptions,
                                                NpgsqlBulkOperationContextFactoryForEntities.Instance,
                                                static query => query,
                                                cancellationToken);
   }

   private async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T, TEntity>(
      IEnumerable<T> entitiesOrValues,
      NpgsqlTempTableBulkInsertOptions options,
      INpgsqlBulkOperationContextFactory bulkOperationContextFactory,
      Func<IQueryable<TEntity>, IQueryable<T>> projection,
      CancellationToken cancellationToken)
      where TEntity : class
   {
      var type = typeof(TEntity);
      var entityTypeName = EntityNameProvider.GetTempTableName(type);
      var entityType = _ctx.Model.GetEntityType(entityTypeName, type);

      var tempTableCreationOptions = options.GetTempTableCreationOptions();
      var primaryKeyCreation = tempTableCreationOptions.PrimaryKeyCreation;

      if (options.MomentOfPrimaryKeyCreation == MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert)
         tempTableCreationOptions.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;

      var tempTableCreator = _ctx.GetService<INpgsqlTempTableCreator>();
      var tempTableReference = await tempTableCreator.CreateTempTableAsync(entityType, tempTableCreationOptions, cancellationToken).ConfigureAwait(false);

      try
      {
         var bulkInsertOptions = options.GetBulkInsertOptions();
         var numberOfInsertedRows = await BulkInsertAsync(entityType, entitiesOrValues, null, tempTableReference.Name, bulkInsertOptions, bulkOperationContextFactory, cancellationToken).ConfigureAwait(false);

         if (options.MomentOfPrimaryKeyCreation == MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert)
         {
            tempTableCreationOptions.PrimaryKeyCreation = primaryKeyCreation;

            var tempTableProperties = tempTableCreationOptions.PropertiesToInclude.DeterminePropertiesForTempTable(entityType);
            var keyProperties = tempTableCreationOptions.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, tempTableProperties);
            await tempTableCreator.CreatePrimaryKeyAsync(_ctx, keyProperties, tempTableReference.Name, tempTableCreationOptions.TruncateTableIfExists, cancellationToken).ConfigureAwait(false);
         }

         var dbSet = entityType.Name == entityTypeName
                        ? _ctx.Set<TEntity>(entityTypeName)
                        : _ctx.Set<TEntity>();

         var query = dbSet.FromTempTable(new TempTableInfo(tempTableReference.Name));

         var pk = entityType.FindPrimaryKey();

         if (pk is not null && pk.Properties.Count != 0)
            query = query.AsNoTracking();

         return new TempTableQuery<T>(projection(query), tempTableReference, numberOfInsertedRows);
      }
      catch (Exception)
      {
         await tempTableReference.DisposeAsync().ConfigureAwait(false);
         throw;
      }
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

      if (options is not NpgsqlTempTableBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlTempTableBulkInsertOptions(options);

      return BulkInsertIntoTempTableAsync<TColumn1, TempTable<TColumn1>>(values,
                                                                         tempTable,
                                                                         npgsqlOptions.GetBulkInsertOptions(),
                                                                         NpgsqlBulkOperationContextFactoryForValues.Instance,
                                                                         cancellationToken);
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

      return BulkInsertIntoTempTableAsync<TColumn1, TempTable<TColumn1>>(values,
                                                                         tempTable,
                                                                         options,
                                                                         NpgsqlBulkOperationContextFactoryForValues.Instance,
                                                                         cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertIntoTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(tempTable);

      if (options is not NpgsqlTempTableBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlTempTableBulkInsertOptions(options);

      return BulkInsertIntoTempTableAsync<T, T>(entities,
                                                tempTable,
                                                npgsqlOptions.GetBulkInsertOptions(),
                                                NpgsqlBulkOperationContextFactoryForEntities.Instance,
                                                cancellationToken);
   }

   /// <inheritdoc />
   public Task BulkInsertIntoTempTableAsync<T>(
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(tempTable);

      return BulkInsertIntoTempTableAsync<T, T>(entities,
                                                tempTable,
                                                options,
                                                NpgsqlBulkOperationContextFactoryForEntities.Instance,
                                                cancellationToken);
   }

   private async Task BulkInsertIntoTempTableAsync<T, TEntity>(
      IEnumerable<T> entitiesOrValues,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      INpgsqlBulkOperationContextFactory bulkOperationContextFactory,
      CancellationToken cancellationToken)
      where TEntity : class
   {
      var type = typeof(TEntity);
      var entityTypeName = EntityNameProvider.GetTempTableName(type);
      var entityType = _ctx.Model.GetEntityType(entityTypeName, type);

      if (options is not NpgsqlBulkInsertOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlBulkInsertOptions(options);

      await BulkInsertAsync(entityType, entitiesOrValues, null, tempTable.Name, npgsqlOptions, bulkOperationContextFactory, cancellationToken).ConfigureAwait(false);
   }

   /// <inheritdoc />
   public Task TruncateTableAsync<T>(CancellationToken cancellationToken = default)
      where T : class
   {
      return TruncateTableAsync(typeof(T), cascade: false, cancellationToken);
   }

   /// <inheritdoc />
   public Task TruncateTableAsync(Type type, CancellationToken cancellationToken = default)
   {
      return TruncateTableAsync(type, cascade: false, cancellationToken);
   }

   /// <inheritdoc />
   public Task TruncateTableAsync<T>(bool cascade, CancellationToken cancellationToken = default)
      where T : class
   {
      return TruncateTableAsync(typeof(T), cascade, cancellationToken);
   }

   /// <inheritdoc />
   public async Task TruncateTableAsync(Type type, bool cascade, CancellationToken cancellationToken = default)
   {
      var entityType = _ctx.Model.GetEntityType(type);
      var tableName = entityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");

      var tableIdentifier = _sqlGenerationHelper.DelimitIdentifier(tableName, entityType.GetSchema());
      var truncateStatement = cascade
                                 ? $"TRUNCATE TABLE {tableIdentifier} CASCADE;"
                                 : $"TRUNCATE TABLE {tableIdentifier};";

      await _ctx.Database.ExecuteSqlRawAsync(truncateStatement, cancellationToken);
   }

   /// <inheritdoc />
   public async Task<int> BulkUpdateAsync<T>(
      IEnumerable<T> entities,
      IBulkUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      if (options is not NpgsqlBulkUpdateOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlBulkUpdateOptions(options);

      return await BulkUpdateAsync(entities, npgsqlOptions, cancellationToken);
   }

   private async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, NpgsqlBulkUpdateOptions options, CancellationToken cancellationToken)
      where T : class
   {
      var entityType = _ctx.Model.GetEntityType(typeof(T));
      var tableName = options.TableName ?? entityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var schema = options.Schema ?? entityType.GetSchema();
      var propertiesForUpdate = options.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, true);

      if (propertiesForUpdate.Count == 0)
         throw new ArgumentException("The number of properties to update cannot be 0.");

      var tempTableOptions = options.GetTempTableBulkInsertOptions();
      await using var tempTable = await BulkInsertIntoTempTableAsync(entities, tempTableOptions, cancellationToken);

      var updateStatement = CreateUpdateCommand(entityType, schema, tableName, tempTable.Name, propertiesForUpdate, options.KeyProperties.DetermineKeyProperties(entityType));

      return await _ctx.Database.ExecuteSqlRawAsync(updateStatement, cancellationToken);
   }

   /// <inheritdoc />
   public async Task<int> BulkInsertOrUpdateAsync<T>(
      IEnumerable<T> entities,
      IBulkInsertOrUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(options);

      if (options is not NpgsqlBulkInsertOrUpdateOptions npgsqlOptions)
         npgsqlOptions = new NpgsqlBulkInsertOrUpdateOptions(options);

      return await BulkInsertOrUpdateAsync(entities, npgsqlOptions, cancellationToken);
   }

   private async Task<int> BulkInsertOrUpdateAsync<T>(IEnumerable<T> entities, NpgsqlBulkInsertOrUpdateOptions options, CancellationToken cancellationToken)
      where T : class
   {
      var entityType = _ctx.Model.GetEntityType(typeof(T));
      var tableName = options.TableName ?? entityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var schema = options.Schema ?? entityType.GetSchema();
      var propertiesForInsert = options.PropertiesToInsert.DeterminePropertiesForInsert(entityType, true);
      var propertiesForUpdate = options.PropertiesToUpdate.DeterminePropertiesForUpdate(entityType, true);

      if (propertiesForInsert.Count == 0)
         throw new ArgumentException("The number of properties to insert cannot be 0.");

      var tempTableOptions = options.GetTempTableBulkInsertOptions();
      await using var tempTable = await BulkInsertIntoTempTableAsync(entities, tempTableOptions, cancellationToken);

      var keyProperties = options.KeyProperties.DetermineKeyProperties(entityType);
      var upsertStatement = CreateInsertOrUpdateCommand(entityType, schema, tableName, tempTable.Name, propertiesForInsert, propertiesForUpdate, keyProperties, options.ConflictDoNothing);

      return await _ctx.Database.ExecuteSqlRawAsync(upsertStatement, cancellationToken);
   }

   private string CreateUpdateCommand(
      IEntityType entityType,
      string? schema,
      string tableName,
      string sourceTempTableName,
      IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
      IReadOnlyList<IProperty> keyProperties)
   {
      var sb = _stringBuilderPool.Get();

      try
      {
         var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
                           ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

         sb.Append("UPDATE ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(tableName, schema))
           .AppendLine(" AS d")
           .Append("SET ");

         var isFirstIteration = true;

         foreach (var property in propertiesToUpdate.Select(p => p.Property).Except(keyProperties))
         {
            if (!isFirstIteration)
               sb.Append(", ");

            var columnName = property.GetColumnName(storeObject)
                             ?? throw new Exception($"Could not get column name for property '{property.Name}' on table '{property.DeclaringType.Name}'.");
            var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);

            sb.Append(escapedColumnName).Append(" = s.").Append(escapedColumnName);
            isFirstIteration = false;
         }

         sb.AppendLine()
           .Append("FROM ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(sourceTempTableName))
           .AppendLine(" AS s")
           .Append("WHERE ");

         isFirstIteration = true;

         foreach (var property in keyProperties)
         {
            if (!isFirstIteration)
               sb.Append(" AND ");

            var columnName = property.GetColumnName(storeObject)
                             ?? throw new Exception($"Could not get column name for property '{property.Name}' on table '{property.DeclaringType.Name}'.");
            var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);

            sb.Append("(d.").Append(escapedColumnName).Append(" = s.").Append(escapedColumnName);

            if (property.IsNullable)
               sb.Append(" OR d.").Append(escapedColumnName).Append(" IS NULL AND s.").Append(escapedColumnName).Append(" IS NULL");

            sb.Append(")");
            isFirstIteration = false;
         }

         sb.Append(_sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }
      finally
      {
         _stringBuilderPool.Return(sb);
      }
   }

   /// <summary>
   /// Performs a bulk update using a server-side query as the source.
   /// </summary>
   /// <param name="sourceQuery">The source query providing update values.</param>
   /// <param name="targetKeySelector">Expression selecting the join key(s) on the target entity.</param>
   /// <param name="sourceKeySelector">Expression selecting the join key(s) on the source entity.</param>
   /// <param name="setPropertyCalls">A function configuring the property assignments.</param>
   /// <param name="filter">An optional predicate to restrict which rows are updated. Receives both target and source entities.</param>
   /// <param name="options">Optional settings to override the target table name or schema.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TTarget">The target entity type.</typeparam>
   /// <typeparam name="TSource">The source entity type.</typeparam>
   /// <typeparam name="TResult">The type of the join key(s).</typeparam>
   /// <returns>The number of rows affected.</returns>
   public async Task<int> BulkUpdateAsync<TTarget, TSource, TResult>(
      IQueryable<TSource> sourceQuery,
      Expression<Func<TTarget, TResult?>> targetKeySelector,
      Expression<Func<TSource, TResult?>> sourceKeySelector,
      Func<SetPropertyBuilder<TTarget, TSource>, SetPropertyBuilder<TTarget, TSource>> setPropertyCalls,
      Expression<Func<TTarget, TSource, bool>>? filter = null,
      NpgsqlBulkUpdateFromQueryOptions? options = null,
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

      var translator = new EfCoreValueExpressionTranslator(_logger.Logger, _ctx);
      var (sql, parameters) = translator.TranslateUpdateFromQuery(
         sourceQuery,
         targetKeySelector,
         sourceKeySelector,
         setClauseBuilder.Entries,
         filter,
         options?.TableName,
         options?.Schema);

      var sqlParams = parameters.Select(kv => (object)new NpgsqlParameter(kv.Key, kv.Value ?? DBNull.Value));
      return await _ctx.Database.ExecuteSqlRawAsync(sql, sqlParams, cancellationToken);
   }

   private string CreateInsertOrUpdateCommand(
      IEntityType entityType,
      string? schema,
      string tableName,
      string sourceTempTableName,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
      IReadOnlyList<IProperty> keyProperties,
      bool conflictDoNothing)
   {
      var sb = _stringBuilderPool.Get();

      try
      {
         var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
                           ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

         // INSERT INTO "TargetTable" ("Col1", "Col2", "Key1")
         sb.Append("INSERT INTO ")
           .Append(_sqlGenerationHelper.DelimitIdentifier(tableName, schema))
           .Append(" (");

         var isFirstIteration = true;

         foreach (var property in propertiesToInsert)
         {
            if (!isFirstIteration)
               sb.Append(", ");

            var columnName = property.GetColumnName(storeObject);
            sb.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
            isFirstIteration = false;
         }

         // SELECT "Col1", "Col2", "Key1" FROM "temp_table"
         sb.Append(")")
           .AppendLine()
           .Append("SELECT ");

         isFirstIteration = true;

         foreach (var property in propertiesToInsert)
         {
            if (!isFirstIteration)
               sb.Append(", ");

            var columnName = property.GetColumnName(storeObject);
            sb.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
            isFirstIteration = false;
         }

         sb.Append(" FROM ")
           .AppendLine(_sqlGenerationHelper.DelimitIdentifier(sourceTempTableName));

         // ON CONFLICT ("Key1") DO NOTHING | DO UPDATE SET ...
         sb.Append("ON CONFLICT (");

         isFirstIteration = true;

         foreach (var property in keyProperties)
         {
            if (!isFirstIteration)
               sb.Append(", ");

            var columnName = property.GetColumnName(storeObject)
                             ?? throw new Exception($"Could not get column name for property '{property.Name}' on table '{property.DeclaringType.Name}'.");
            sb.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
            isFirstIteration = false;
         }

         if (conflictDoNothing)
         {
            sb.Append(") DO NOTHING");
         }
         else
         {
            sb.Append(") DO UPDATE SET ");

            isFirstIteration = true;

            foreach (var property in propertiesToUpdate.Select(p => p.Property).Except(keyProperties))
            {
               if (!isFirstIteration)
                  sb.Append(", ");

               var columnName = property.GetColumnName(storeObject)
                                ?? throw new Exception($"Could not get column name for property '{property.Name}' on table '{property.DeclaringType.Name}'.");
               var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);

               sb.Append(escapedColumnName).Append(" = EXCLUDED.").Append(escapedColumnName);
               isFirstIteration = false;
            }
         }

         sb.Append(_sqlGenerationHelper.StatementTerminator);

         return sb.ToString();
      }
      finally
      {
         _stringBuilderPool.Return(sb);
      }
   }

   /// <summary>
   /// Performs a bulk insert using a server-side query as the source.
   /// </summary>
   /// <param name="sourceQuery">The source query providing insert values.</param>
   /// <param name="mapPropertyCalls">A function configuring the column mappings.</param>
   /// <param name="options">Optional settings to override the target table name or schema.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TTarget">The target entity type.</typeparam>
   /// <typeparam name="TSource">The source entity type.</typeparam>
   /// <returns>The number of rows affected.</returns>
   public async Task<int> BulkInsertAsync<TTarget, TSource>(
      IQueryable<TSource> sourceQuery,
      Func<InsertPropertyBuilder<TTarget, TSource>, InsertPropertyBuilder<TTarget, TSource>> mapPropertyCalls,
      NpgsqlBulkInsertFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(mapPropertyCalls);

      var builder = mapPropertyCalls(new InsertPropertyBuilder<TTarget, TSource>());

      if (builder.Entries.Count == 0)
         throw new ArgumentException("At least one column mapping is required.", nameof(mapPropertyCalls));

      var translator = new EfCoreValueExpressionTranslator(_logger.Logger, _ctx);
      var (sql, parameters) = translator.TranslateInsertFromQuery<TTarget, TSource>(
                                                                                    sourceQuery,
                                                                                    builder.Entries,
                                                                                    options?.TableName,
                                                                                    options?.Schema);

      var sqlParams = parameters.Select(kv => (object)new NpgsqlParameter(kv.Key, kv.Value ?? DBNull.Value));
      return await _ctx.Database.ExecuteSqlRawAsync(sql, sqlParams, cancellationToken);
   }
}
