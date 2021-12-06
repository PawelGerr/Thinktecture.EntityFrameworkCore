using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="SelectExpression"/>.
/// </summary>
public static class SelectExpressionExtensions
{
   /// <summary>
   /// Determines whether the provided <paramref name="selectExpression"/> should be used for generation of a DELETE statement.
   /// </summary>
   /// <param name="selectExpression">Expression to check.</param>
   /// <param name="tableToDeleteIn">Table to execute DELETE on.</param>
   /// <returns><c>true</c> if a DELETE statement should be rendered; <c>false</c> otherwise.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="selectExpression"/> is <c>null</c>.</exception>
   public static bool TryGetDeleteExpression(this SelectExpression selectExpression, [NotNullWhen(true)] out TableExpressionBase? tableToDeleteIn)
   {
      if (selectExpression == null)
         throw new ArgumentNullException(nameof(selectExpression));

      if (selectExpression.Projection.Count == 1 &&
          selectExpression.Projection[0].Expression is DeleteExpression deleteExpression)
      {
         tableToDeleteIn = deleteExpression.Table;
         return true;
      }

      tableToDeleteIn = default;
      return false;
   }
}
