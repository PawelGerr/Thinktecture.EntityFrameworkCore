using System.Collections.Generic;
using System.Linq;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
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
   }
}
