using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Gives EF a hint that window functions are not evaluatable.
/// </summary>
public class WindowFunctionEvaluatableExpressionFilterPlugin : IEvaluatableExpressionFilterPlugin
{
   /// <inheritdoc />
   public bool IsEvaluatableExpression(Expression expression)
   {
      if (expression is MethodCallExpression methodCallExpression)
      {
         if (methodCallExpression.Method.DeclaringType == typeof(SqlServerDbFunctionsExtensions)
             && methodCallExpression.Method.Name == nameof(SqlServerDbFunctionsExtensions.WindowFunction))
            return false;
      }
      return true;
   }
}
