using System;
using System.Collections.Generic;
using System.Linq;
using Thinktecture.Collections;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="IEnumerable{T}"/>.
   /// </summary>
   public static class EnumerableExtensions
   {
      /// <summary>
      /// Creates an <see cref="IQueryable{T}"/> that implements <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <param name="collection">A collection to make an <see cref="IQueryable{T}"/> from.</param>
      /// <typeparam name="T">Item type.</typeparam>
      /// <returns>An implementation of <see cref="IQueryable{T}"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
      public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> collection)
      {
         return new AsyncEnumerable<T>(collection);
      }
   }
}
