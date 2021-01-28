using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="SelectExpression"/>.
   /// </summary>
   public static class SelectExpressionExtensions
   {
      /// <summary>
      /// Determines whether the provided <paramref name="selectExpression"/> should be used for generation of a DELETE statement.
      /// </summary>
      /// <param name="selectExpression">Expression to check.</param>
      /// <param name="deleteExpression">An instance of <see cref="DeleteExpression"/>.</param>
      /// <returns><c>true</c> if a DELETE statement should be rendered; <c>false</c> otherwise.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="selectExpression"/> is <c>null</c>.</exception>
      public static bool TryGetDeleteExpression(this SelectExpression selectExpression, [NotNullWhen(true)] out DeleteExpression? deleteExpression)
      {
         if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

         if (selectExpression.Projection.Count == 1 &&
             selectExpression.Projection[0].Expression is DeleteExpression deleteExpr)
         {
            deleteExpression = deleteExpr;
            return true;
         }

         deleteExpression = default;
         return false;
      }
   }
}
