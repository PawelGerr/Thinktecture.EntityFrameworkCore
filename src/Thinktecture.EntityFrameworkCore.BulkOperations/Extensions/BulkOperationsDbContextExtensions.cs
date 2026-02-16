using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbContext"/>.
/// </summary>
public static class BulkOperationsDbContextExtensions
{
   /// <summary>
   /// Creates a temp table using custom type '<typeparamref name="T"/>'.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of custom temp table.</typeparam>
   /// <returns>Table name</returns>
   /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by the provided <paramref name="ctx"/>.</exception>
   public static Task<ITempTableReference> CreateTempTableAsync<T>(
      this DbContext ctx,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.CreateTempTableAsync(typeof(T), options, cancellationToken);
   }

   /// <summary>
   /// Creates a temp table.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="type">Type of the entity.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Table name</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="ctx"/> is <c>null</c>
   /// - or  <paramref name="type"/> is <c>null</c>
   /// - or  <paramref name="options"/> is <c>null</c>.
   /// </exception>
   /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
   public static Task<ITempTableReference> CreateTempTableAsync(
      this DbContext ctx,
      Type type,
      ITempTableCreationOptions options,
      CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      var entityType = ctx.Model.GetEntityType(EntityNameProvider.GetTempTableName(type), type);
      return ctx.GetService<ITempTableCreator>().CreateTempTableAsync(entityType, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkInsertExecutor = ctx.GetService<IBulkInsertExecutor>();
      var options = bulkInsertExecutor.CreateOptions(propertiesToInsert is null ? null : IEntityPropertiesProvider.Include(propertiesToInsert));

      return await bulkInsertExecutor.BulkInsertAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkInsertOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return await ctx.GetService<IBulkInsertExecutor>()
                      .BulkInsertAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> in the table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to update.</param>
   /// <param name="propertiesToUpdate">Properties to update. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToMatchOn">Properties to match on. If <c>null</c> then the primary key of the entity is used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToUpdate = null,
      Expression<Func<T, object?>>? propertiesToMatchOn = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkUpdateExecutor = ctx.GetService<IBulkUpdateExecutor>();

      var options = bulkUpdateExecutor.CreateOptions(propertiesToUpdate is null ? null : IEntityPropertiesProvider.Include(propertiesToUpdate),
                                                     propertiesToMatchOn is null ? null : IEntityPropertiesProvider.Include(propertiesToMatchOn));

      return await bulkUpdateExecutor.BulkUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> in the table.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to update.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return await ctx.GetService<IBulkUpdateExecutor>()
                      .BulkUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> that are in the table, the rest will be inserted.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert or update.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToUpdate">Properties to update. If <c>null</c> then all properties are used.</param>
   /// <param name="propertiesToMatchOn">Properties to match on. If <c>null</c> then the primary key of the entity is used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertOrUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      Expression<Func<T, object?>>? propertiesToUpdate = null,
      Expression<Func<T, object?>>? propertiesToMatchOn = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var bulkOperationExecutor = ctx.GetService<IBulkInsertOrUpdateExecutor>();

      var options = bulkOperationExecutor.CreateOptions(propertiesToInsert is null ? null : IEntityPropertiesProvider.Include(propertiesToInsert),
                                                        propertiesToUpdate is null ? null : IEntityPropertiesProvider.Include(propertiesToUpdate),
                                                        propertiesToMatchOn is null ? null : IEntityPropertiesProvider.Include(propertiesToMatchOn));

      return await bulkOperationExecutor.BulkInsertOrUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Updates <paramref name="entities"/> that are in the table, the rest will be inserted.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert or update.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Number of affected rows.</returns>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static async Task<int> BulkInsertOrUpdateAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      IBulkInsertOrUpdateOptions options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return await ctx.GetService<IBulkInsertOrUpdateExecutor>()
                      .BulkInsertOrUpdateAsync(entities, options, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
      this DbContext ctx,
      IEnumerable<TColumn1> values,
      ITempTableBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      options ??= executor.CreateOptions();

      return executor.BulkInsertValuesIntoTempTableAsync(values, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<TempTable<TColumn1, TColumn2>>> BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(
      this DbContext ctx,
      IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
      ITempTableBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

      return ctx.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      var options = executor.CreateOptions(propertiesToInsert is null ? null : IEntityPropertiesProvider.Include(propertiesToInsert));

      return executor.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      options ??= executor.CreateOptions();

      return executor.BulkInsertIntoTempTableAsync(entities, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task BulkInsertValuesIntoTempTableAsync<TColumn1>(
      this DbContext ctx,
      IEnumerable<TColumn1> values,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();

      return executor.BulkInsertValuesIntoTempTableAsync(values, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the values to insert.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task BulkInsertValuesIntoTempTableAsync<TColumn1>(
      this DbContext ctx,
      IEnumerable<TColumn1> values,
      ITempTableReference tempTable,
      IBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();

      return executor.BulkInsertValuesIntoTempTableAsync(values, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(
      this DbContext ctx,
      IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

      return ctx.BulkInsertIntoTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="values"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="values">Values to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="values"/> is <c>null</c>.</exception>
   public static Task BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(
      this DbContext ctx,
      IEnumerable<(TColumn1 column1, TColumn2 column2)> values,
      ITempTableReference tempTable,
      IBulkInsertOptions? options = null,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(values);

      var entities = values.Select(t => new TempTable<TColumn1, TColumn2>(t.column1, t.column2));

      return ctx.BulkInsertIntoTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="propertiesToInsert">Properties to insert. If <c>null</c> then all properties are used.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>A query for accessing the inserted values.</returns>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      Expression<Func<T, object?>>? propertiesToInsert = null,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();
      var options = executor.CreateBulkInsertOptions(propertiesToInsert is null ? null : IEntityPropertiesProvider.Include(propertiesToInsert));

      return executor.BulkInsertIntoTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      ITempTableBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();

      return executor.BulkInsertIntoTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Copies <paramref name="entities"/> into a temp table and returns the query for accessing the inserted records.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="entities">Entities to insert.</param>
   /// <param name="tempTable">Temp table to insert into.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
   public static Task BulkInsertIntoTempTableAsync<T>(
      this DbContext ctx,
      IEnumerable<T> entities,
      ITempTableReference tempTable,
      IBulkInsertOptions? options,
      CancellationToken cancellationToken = default)
      where T : class
   {
      var executor = ctx.GetService<ITempTableBulkInsertExecutor>();

      return executor.BulkInsertIntoTempTableAsync(entities, tempTable, options, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   public static Task TruncateTableAsync<T>(
      this DbContext ctx,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.GetService<ITruncateTableExecutor>()
                .TruncateTableAsync<T>(cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/>.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   public static Task TruncateTableAsync(
      this DbContext ctx,
      Type type,
      CancellationToken cancellationToken = default)
   {
      return ctx.GetService<ITruncateTableExecutor>()
                .TruncateTableAsync(type, cancellationToken);
   }

   /// <summary>
   /// Converts the provided <paramref name="values"/> to a "parameter" to be used in queries.
   /// </summary>
   /// <param name="ctx">An instance of <see cref="DbContext"/> to use the <paramref name="values"/> with.</param>
   /// <param name="values">A collection of <paramref name="values"/> to create a query from.</param>
   /// <param name="applyDistinct">
   /// Indication whether the query should apply 'DISTINCT' on <paramref name="values"/>.
   /// It is highly recommended to set this parameter to <c>true</c> to get better execution plans.
   /// </param>
   /// <typeparam name="T">Type of the <paramref name="values"/>.</typeparam>
   /// <returns>An <see cref="IQueryable{T}"/> giving access to the provided <paramref name="values"/>.</returns>
   public static IQueryable<T> CreateScalarCollectionParameter<T>(this DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct = true)
   {
      return ctx.GetService<ICollectionParameterFactory>()
                .CreateScalarQuery(ctx, values, applyDistinct);
   }

   /// <summary>
   /// Converts the provided <paramref name="objects"/> to a "parameter" to be used in queries.
   /// </summary>
   /// <param name="ctx">An instance of <see cref="DbContext"/> to use the <paramref name="objects"/> with.</param>
   /// <param name="objects">A collection of <paramref name="objects"/> to create a query from.</param>
   /// <param name="applyDistinct">
   /// Indication whether the query should apply 'DISTINCT' on <paramref name="objects"/>.
   /// It is highly recommended to set this parameter to <c>true</c> to get better execution plans.
   /// </param>
   /// <typeparam name="T">Type of the <paramref name="objects"/>.</typeparam>
   /// <returns>An <see cref="IQueryable{T}"/> giving access to the provided <paramref name="objects"/>.</returns>
   public static IQueryable<T> CreateComplexCollectionParameter<T>(this DbContext ctx, IReadOnlyCollection<T> objects, bool applyDistinct = true)
      where T : class
   {
      return ctx.GetService<ICollectionParameterFactory>()
                .CreateComplexQuery(ctx, objects, applyDistinct);
   }
}
