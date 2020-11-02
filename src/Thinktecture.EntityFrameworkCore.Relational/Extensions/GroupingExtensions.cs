using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="IGrouping{TKey,TElement}"/>.
   /// </summary>
   public static class GroupingExtensions
   {
      /// <summary>
      /// Performs 'COUNT(DISTINCT column)' when used in with Entity Framework Core.
      /// </summary>
      /// <param name="grouping">Source query.</param>
      /// <param name="selector">Selects the column to distinct by.</param>
      /// <typeparam name="T">Type of the source item.</typeparam>
      /// <typeparam name="TKey">Type of the key.</typeparam>
      /// <typeparam name="TProp">Type of the column to distinct by.</typeparam>
      /// <returns>Count.</returns>
      /// <exception cref="InvalidOperationException">If used not in a projection with Entity Framework Core.</exception>
#pragma warning disable CA1801
      public static int CountDistinct<T, TKey, TProp>(this IGrouping<TKey, T> grouping, Func<T, TProp> selector)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }
#pragma warning restore CA1801

   }
}
