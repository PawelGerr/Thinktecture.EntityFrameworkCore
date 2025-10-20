using System.Linq.Expressions;
using System.Reflection;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class RelationalQueryableExtensions
{
   private static readonly MethodInfo _asSubQuery = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                         .Single(m => m.Name == nameof(AsSubQuery) && m.IsGenericMethod);

   private static readonly MethodInfo _withTableHints = typeof(RelationalQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                             .Single(m => m.Name == nameof(WithTableHints)
                                                                                                          && m.IsGenericMethod
                                                                                                          && m.GetParameters()[1].ParameterType == typeof(IReadOnlyList<ITableHint>));

   /// <summary>
   /// Adds table hints to a table specified in <paramref name="source"/>.
   /// </summary>
   /// <param name="source">Query using a table to apply table hints to.</param>
   /// <param name="hints">Table hints.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>Query with table hints applied.</returns>
   public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, params ITableHint[] hints)
   {
      return source.WithTableHints((IReadOnlyList<ITableHint>)hints);
   }

   /// <summary>
   /// Adds table hints to a table specified in <paramref name="source"/>.
   /// </summary>
   /// <param name="source">Query using a table to apply table hints to.</param>
   /// <param name="hints">Table hints.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>Query with table hints applied.</returns>
   public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, IReadOnlyList<ITableHint> hints)
   {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(hints);

      var methodInfo = _withTableHints.MakeGenericMethod(typeof(T));
      var expression = Expression.Call(null, methodInfo, source.Expression, new TableHintsExpression(hints));
      return source.Provider.CreateQuery<T>(expression);
   }

   /// <summary>
   /// Executes provided query as a sub query.
   /// </summary>
   /// <param name="source">Query to execute as as sub query.</param>
   /// <typeparam name="TEntity">Type of the entity.</typeparam>
   /// <returns>Query that will be executed as a sub query.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
   public static IQueryable<TEntity> AsSubQuery<TEntity>(this IQueryable<TEntity> source)
   {
      ArgumentNullException.ThrowIfNull(source);

      return source.Provider.CreateQuery<TEntity>(Expression.Call(null, _asSubQuery.MakeGenericMethod(typeof(TEntity)), source.Expression));
   }
}
