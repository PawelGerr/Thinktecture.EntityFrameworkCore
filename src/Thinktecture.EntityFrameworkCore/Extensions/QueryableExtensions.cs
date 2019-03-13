using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Thinktecture.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class QueryableExtensions
   {
      [NotNull]
      public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
         [NotNull] this IQueryable<TOuter> outer,
         [NotNull] IEnumerable<TInner> inner,
         [NotNull] Expression<Func<TOuter, TKey>> outerKeySelector,
         [NotNull] Expression<Func<TInner, TKey>> innerKeySelector,
         [NotNull] Expression<Func<LeftJoinResult<TOuter, TInner>, TResult>> resultSelector)
      {
         if (outer == null)
            throw new ArgumentNullException(nameof(outer));
         if (inner == null)
            throw new ArgumentNullException(nameof(inner));
         if (outerKeySelector == null)
            throw new ArgumentNullException(nameof(outerKeySelector));
         if (innerKeySelector == null)
            throw new ArgumentNullException(nameof(innerKeySelector));
         if (resultSelector == null)
            throw new ArgumentNullException(nameof(resultSelector));

         return outer
                .GroupJoin(inner, outerKeySelector, innerKeySelector, (o, i) => new { Outer = o, Inner = i })
                .SelectMany(g => g.Inner.DefaultIfEmpty(), (o, i) => new LeftJoinResult<TOuter, TInner>
                                                                     {
                                                                        Outer = o.Outer,
                                                                        Inner = i
                                                                     })
                .Select(resultSelector);
      }
   }
}
