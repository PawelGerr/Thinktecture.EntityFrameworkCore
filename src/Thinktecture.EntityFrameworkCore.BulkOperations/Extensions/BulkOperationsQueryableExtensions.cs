using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/>.
/// </summary>
public static class BulkOperationsQueryableExtensions
{
   private static readonly MethodInfo _bulkDelete = typeof(BulkOperationsQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                             .Single(m => m.Name == nameof(BulkDelete) && m.IsGenericMethod);

   /// <summary>
   /// Deletes Entity Framework entities of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="source">Entities to delete.</param>
   /// <typeparam name="T">Type of the entities to delete.</typeparam>
   /// <returns>Number of deleted entities.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
   public static int BulkDelete<T>(this IQueryable<T> source)
   {
      if (source == null)
         throw new ArgumentNullException(nameof(source));

      var methodInfo = _bulkDelete.MakeGenericMethod(typeof(T));
      var expression = Expression.Call(null, methodInfo, source.Expression);
      return source.Provider.Execute<int>(expression);
   }

   /// <summary>
   /// Deletes Entity Framework entities of type <typeparamref name="T"/>.
   /// </summary>
   /// <param name="source">Entities to delete.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entities to delete.</typeparam>
   /// <returns>Number of deleted entities.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">The underlying query provider is not an <see cref="IAsyncQueryProvider"/>.</exception>
   public static Task<int> BulkDeleteAsync<T>(
      this IQueryable<T> source,
      CancellationToken cancellationToken = default)
   {
      if (source == null)
         throw new ArgumentNullException(nameof(source));

      if (source.Provider is not IAsyncQueryProvider provider)
         throw new InvalidOperationException(CoreStrings.IQueryableProviderNotAsync);

      var methodInfo = _bulkDelete.MakeGenericMethod(typeof(T));
      var expression = Expression.Call(null, methodInfo, source.Expression);
      return provider.ExecuteAsync<Task<int>>(expression, cancellationToken);
   }
}