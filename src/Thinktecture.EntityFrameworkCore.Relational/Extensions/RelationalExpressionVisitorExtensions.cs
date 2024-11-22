using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="ExpressionVisitor"/>.
/// </summary>
public static class RelationalExpressionVisitorExtensions
{
   /// <summary>
   /// Visits a collection of <paramref name="expressions"/> and returns new collection if it least one expression has been changed.
   /// Otherwise the provided <paramref name="expressions"/> are returned if there are no changes.
   /// </summary>
   /// <param name="visitor">Visitor to use.</param>
   /// <param name="expressions">Expressions to visit.</param>
   /// <returns>
   /// New collection with visited expressions if at least one visited expression has been changed; otherwise the provided <paramref name="expressions"/>.
   /// </returns>
   public static IReadOnlyList<T> VisitExpressions<T>(this ExpressionVisitor visitor, IReadOnlyList<T> expressions)
      where T : Expression
   {
      T[]? visitedExpression = null;

      for (var i = 0; i < expressions.Count; i++)
      {
         var expression = expressions[i];
         var visited = (T)visitor.Visit(expression);

         if (visited != expression && visitedExpression is null)
         {
            visitedExpression = new T[expressions.Count];

            for (var j = 0; j < i; j++)
            {
               visitedExpression[j] = expressions[j];
            }
         }

         if (visitedExpression is not null)
            visitedExpression[i] = visited;
      }

      return visitedExpression ?? expressions;
   }
}
