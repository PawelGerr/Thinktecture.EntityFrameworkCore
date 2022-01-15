using System.Linq.Expressions;
using System.Reflection;
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
      ArgumentNullException.ThrowIfNull(source);

      var methodInfo = _bulkDelete.MakeGenericMethod(typeof(T));
      var sourceExpression = IncludeRemovingVisitor.Instance.Visit(source.Expression);
      var expression = Expression.Call(null, methodInfo, sourceExpression);
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
      ArgumentNullException.ThrowIfNull(source);

      if (source.Provider is not IAsyncQueryProvider provider)
         throw new InvalidOperationException(CoreStrings.IQueryableProviderNotAsync);

      var methodInfo = _bulkDelete.MakeGenericMethod(typeof(T));
      var sourceExpression = IncludeRemovingVisitor.Instance.Visit(source.Expression);
      var expression = Expression.Call(null, methodInfo, sourceExpression);
      return provider.ExecuteAsync<Task<int>>(expression, cancellationToken);
   }

   private class IncludeRemovingVisitor : ExpressionVisitor
   {
      public static readonly IncludeRemovingVisitor Instance = new();

      protected override Expression VisitMethodCall(MethodCallExpression node)
      {
         // Include and ThenInclude may expand the query so it is unclear what table to DELETE from
         if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions)
             && node.Method.Name is nameof(EntityFrameworkQueryableExtensions.Include) or nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            return node.Arguments[0];

         return base.VisitMethodCall(node);
      }
   }
}
