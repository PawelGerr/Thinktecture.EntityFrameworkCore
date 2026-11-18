using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Prevents MySQL window-function expressions from being evaluated on the client.
/// </summary>
public sealed class MySqlWindowFunctionEvaluatableExpressionFilterPlugin : IEvaluatableExpressionFilterPlugin
{
   /// <inheritdoc />
   public bool IsEvaluatableExpression(Expression expression)
   {
      return expression is not MethodCallExpression methodCallExpression
             || methodCallExpression.Method.DeclaringType != typeof(MySqlDbFunctionsExtensions)
             || methodCallExpression.Method.Name is not (nameof(MySqlDbFunctionsExtensions.WindowFunction) or nameof(MySqlDbFunctionsExtensions.NTile));
   }
}