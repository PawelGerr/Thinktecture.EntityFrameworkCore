using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbSet{TEntity}"/> to support query-based bulk update on SQL Server.
/// </summary>
public static class SqlServerBulkOperationsDbSetExtensions
{
   /// <summary>
   /// Performs a bulk update on the target <see cref="DbSet{TTarget}"/> using a server-side query as the source.
   /// </summary>
   /// <param name="target">The target DbSet to update.</param>
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
   public static async Task<int> BulkUpdateAsync<TTarget, TSource, TResult>(
      this DbSet<TTarget> target,
      IQueryable<TSource> sourceQuery,
      Expression<Func<TTarget, TResult?>> targetKeySelector,
      Expression<Func<TSource, TResult?>> sourceKeySelector,
      Func<SetPropertyBuilder<TTarget, TSource>, SetPropertyBuilder<TTarget, TSource>> setPropertyCalls,
      Expression<Func<TTarget, TSource, bool>>? filter = null,
      SqlServerBulkUpdateFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(targetKeySelector);
      ArgumentNullException.ThrowIfNull(sourceKeySelector);
      ArgumentNullException.ThrowIfNull(setPropertyCalls);

      var executor = target.GetService<SqlServerBulkOperationExecutor>();

      return await executor.BulkUpdateAsync(sourceQuery, targetKeySelector, sourceKeySelector, setPropertyCalls, filter, options, cancellationToken)
                           .ConfigureAwait(false);
   }

   /// <summary>
   /// Performs a bulk insert into the target <see cref="DbSet{TTarget}"/> using a server-side query as the source.
   /// </summary>
   /// <param name="target">The target DbSet to insert into.</param>
   /// <param name="sourceQuery">The source query providing insert values.</param>
   /// <param name="mapPropertyCalls">A function configuring the column mappings.</param>
   /// <param name="options">Optional settings to override the target table name or schema.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="TTarget">The target entity type.</typeparam>
   /// <typeparam name="TSource">The source entity type.</typeparam>
   /// <returns>The number of rows affected.</returns>
   public static async Task<int> BulkInsertAsync<TTarget, TSource>(
      this DbSet<TTarget> target,
      IQueryable<TSource> sourceQuery,
      Func<InsertPropertyBuilder<TTarget, TSource>, InsertPropertyBuilder<TTarget, TSource>> mapPropertyCalls,
      SqlServerBulkInsertFromQueryOptions? options = null,
      CancellationToken cancellationToken = default)
      where TTarget : class
      where TSource : class
   {
      ArgumentNullException.ThrowIfNull(target);
      ArgumentNullException.ThrowIfNull(sourceQuery);
      ArgumentNullException.ThrowIfNull(mapPropertyCalls);

      var executor = target.GetService<SqlServerBulkOperationExecutor>();

      return await executor.BulkInsertAsync(sourceQuery, mapPropertyCalls, options, cancellationToken)
                           .ConfigureAwait(false);
   }
}
