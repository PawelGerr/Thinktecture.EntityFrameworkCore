using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Prevents evaluation of window function expressions on the client side.
/// </summary>
public class NpgsqlWindowFunctionEvaluatableExpressionFilterPlugin : IEvaluatableExpressionFilterPlugin
{
   /// <inheritdoc />
   public bool IsEvaluatableExpression(Expression expression)
   {
      if (expression is MethodCallExpression methodCallExpression)
      {
         if (methodCallExpression.Method.DeclaringType == typeof(NpgsqlDbFunctionsExtensions)
             && methodCallExpression.Method.Name == nameof(NpgsqlDbFunctionsExtensions.WindowFunction))
            return false;
      }

      return true;
   }
}
