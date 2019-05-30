using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="ExpressionVisitor"/>.
   /// </summary>
   public static class ExpressionVisitorExtensions
   {
      /// <summary>
      /// Visits a collection of <paramref name="expressions"/> and returns new collection if it least one expression has been changed.
      /// The provided <paramref name="expressions"/> are returned if there are no changes.
      /// </summary>
      /// <param name="visitor">Visitor to use.</param>
      /// <param name="expressions">Expressions to visit.</param>
      /// <returns>
      /// New collection with visited expressions if at least one visited expression has been changed; otherwise the provided <paramref name="expressions"/>.
      /// </returns>
      [NotNull]
      public static IReadOnlyCollection<Expression> VisitExpressions([NotNull] this ExpressionVisitor visitor, [NotNull] IReadOnlyCollection<Expression> expressions)
      {
         if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));
         if (expressions == null)
            throw new ArgumentNullException(nameof(expressions));

         var visitedExpressions = new List<Expression>();
         var hasChanges = false;

         foreach (var expression in expressions)
         {
            var visitedExpression = visitor.Visit(expression);
            visitedExpressions.Add(visitedExpression);
            hasChanges |= !ReferenceEquals(visitedExpression, expression);
         }

         return hasChanges ? visitedExpressions.AsReadOnly() : expressions;
      }
   }
}
