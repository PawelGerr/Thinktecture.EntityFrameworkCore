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
      if (expression is MethodCallExpression methodCallExpression
          && methodCallExpression.Method.DeclaringType == typeof(SqlServerDbFunctionsExtensions))
      {
         if (methodCallExpression.Method.Name is nameof(SqlServerDbFunctionsExtensions.WindowFunction)
                                              or nameof(SqlServerDbFunctionsExtensions.NTile))
            return false;
      }
      return true;
   }
}
