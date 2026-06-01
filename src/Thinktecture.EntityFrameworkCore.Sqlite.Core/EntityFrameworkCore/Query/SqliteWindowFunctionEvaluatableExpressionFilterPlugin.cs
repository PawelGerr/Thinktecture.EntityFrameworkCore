using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Prevents evaluation of window function expressions on the client side.
/// </summary>
public class SqliteWindowFunctionEvaluatableExpressionFilterPlugin : IEvaluatableExpressionFilterPlugin
{
   /// <inheritdoc />
   public bool IsEvaluatableExpression(Expression expression)
   {
      if (expression is MethodCallExpression methodCallExpression
          && methodCallExpression.Method.DeclaringType == typeof(SqliteDbFunctionsExtensions))
      {
         if (methodCallExpression.Method.Name == nameof(SqliteDbFunctionsExtensions.NTile))
            return false;
      }

      return true;
   }
}
