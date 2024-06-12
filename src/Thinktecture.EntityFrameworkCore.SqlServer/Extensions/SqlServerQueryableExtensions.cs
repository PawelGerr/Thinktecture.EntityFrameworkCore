using System.Linq.Expressions;

using Thinktecture.EntityFrameworkCore;

namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/>.
/// </summary>
public static class SqlServerQueryableExtensions
{
   /// <summary>
   /// Adds table hints to a table specified in <paramref name="source"/>.
   /// </summary>
   /// <param name="source">Query using a table to apply table hints to.</param>
   /// <param name="hints">Table hints.</param>
   /// <typeparam name="T">Entity type.</typeparam>
   /// <returns>Query with table hints applied.</returns>
   public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, params SqlServerTableHint[] hints)
   {
      return source.WithTableHints((IReadOnlyList<ITableHint>)hints);
   }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="source"></param>
    /// <param name="propertyExpression"></param>
    /// <returns></returns>
    public static IQueryable<T> IgnoreProperty<T, TProp>(this IQueryable<T> source, Expression<Func<T, TProp>> propertyExpression)
    {
        return source.IgnoreProperties([((MemberExpression)propertyExpression.Body).Member.Name]);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="source"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static IQueryable<T> IgnoreProperty<T, TProp>(this IQueryable<T> source, string propertyName)
    {
        return source.IgnoreProperties([propertyName]);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="source"></param>
    /// <param name="propertyExpressions"></param>
    /// <returns></returns>
    public static IQueryable<T> IgnoreProperties<T, TProp>(this IQueryable<T> source, params Expression<Func<T, TProp>>[] propertyExpressions)
    {
        return source.IgnoreProperties(propertyExpressions.Select(x => ((MemberExpression)x.Body).Member.Name).ToArray());
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="source"></param>
    /// <param name="propertyNames"></param>
    /// <returns></returns>
    public static IQueryable<T> IgnoreProperties<T, TProp>(this IQueryable<T> source, params string[] propertyNames)
    {
        return source.IgnoreProperties(propertyNames);
    }

}
