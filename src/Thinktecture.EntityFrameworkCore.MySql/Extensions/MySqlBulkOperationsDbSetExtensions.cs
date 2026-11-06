using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// MySQL query-based bulk-operation extensions for <see cref="DbSet{TEntity}" />.
/// </summary>
public static class MySqlBulkOperationsDbSetExtensions
{
   /// <summary>
   /// Updates a target set using values from a server-side source query.
   /// </summary>
   /// <typeparam name="TTarget">Type of the entity to update.</typeparam>
   /// <typeparam name="TSource">Type of the source query's entity.</typeparam>
   /// <typeparam name="TResult">Type of the join key.</typeparam>
   /// <param name="target">The set to update.</param>
   /// <param name="sourceQuery">Query providing the source rows.</param>
   /// <param name="targetKeySelector">Selects the key of the target entity.</param>
   /// <param name="sourceKeySelector">Selects the key of the source entity.</param>
   /// <param name="setPropertyCalls">Configures the property assignments.</param>
   /// <param name="filter">Optional condition restricting the updated rows.</param>
   /// <param name="options">Optional options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   public static Task<int> BulkUpdateAsync<TTarget, TSource, TResult>(
      this DbSet<TTarget> target,
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
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(targetKeySelector);
      ArgumentNullException.ThrowIfNull(sourceKeySelector);
      ArgumentNullException.ThrowIfNull(setPropertyCalls);

      return target.GetService<MySqlBulkOperationExecutor>()
                   .BulkUpdateAsync(sourceQuery, targetKeySelector, sourceKeySelector, setPropertyCalls, filter, options, cancellationToken);
   }

   /// <summary>
   /// Inserts values from a server-side source query into the target set.
   /// </summary>
   /// <typeparam name="TTarget">Type of the entity to insert.</typeparam>
   /// <typeparam name="TSource">Type of the source query's entity.</typeparam>
   /// <param name="target">The set to insert into.</param>
   /// <param name="sourceQuery">Query providing the source rows.</param>
   /// <param name="mapPropertyCalls">Configures the column mappings.</param>
   /// <param name="options">Optional options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   public static Task<int> BulkInsertAsync<TTarget, TSource>(
      this DbSet<TTarget> target,
      IQueryable<TSource> sourceQuery,
      Func<InsertPropertyBuilder<TTarget, TSource>, InsertPropertyBuilder<TTarget, TSource>> mapPropertyCalls,
      MySqlBulkInsertFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(mapPropertyCalls);

      return target.GetService<MySqlBulkOperationExecutor>()
                   .BulkInsertAsync(sourceQuery, mapPropertyCalls, options, cancellationToken);
   }
}