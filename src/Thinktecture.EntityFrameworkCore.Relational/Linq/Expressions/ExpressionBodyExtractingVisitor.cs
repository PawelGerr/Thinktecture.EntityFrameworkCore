using System.Linq.Expressions;
using System.Reflection;

namespace Thinktecture.Linq.Expressions;

/// <summary>
/// Searches for occurrences of <see cref="RelationalExpressionExtensions.ExtractBody{TIn,TOut}"/>
/// and replaces them with the body of the provided lambda expression.
/// The references to the original lambda parameter is replaced with the one provided with <see cref="RelationalExpressionExtensions.ExtractBody{TIn,TOut}"/>.
/// </summary>
public class ExpressionBodyExtractingVisitor : ExpressionVisitor
{
   private static readonly ExpressionBodyExtractingVisitor _instance = new();
   private static readonly MethodInfo _extractBodyMethod = typeof(RelationalExpressionExtensions).GetMethod(nameof(RelationalExpressionExtensions.ExtractBody), BindingFlags.Static | BindingFlags.Public)
                                                           ?? throw new Exception($"Method '{nameof(RelationalExpressionExtensions.ExtractBody)}' not found.");

   /// <summary>
   /// Rewrites the provided <paramref name="expression"/> if it contains <see cref="RelationalExpressionExtensions.ExtractBody{TIn,TOut}"/>.
   /// </summary>
   /// <param name="expression">Expression to rewrite.</param>
   /// <typeparam name="T">The type of the lambda expression.</typeparam>
   /// <returns>
   /// A rewritten <paramref name="expression"/> if any occurrences of <see cref="RelationalExpressionExtensions.ExtractBody{TIn,TOut}"/> have been found;
   /// otherwise the provided <paramref name="expression"/> is returned.
   /// </returns>
   /// <exception cref="NotSupportedException">The provided <paramref name="expression"/> could not be rewritten.</exception>
   public static T Rewrite<T>(T expression)
      where T : LambdaExpression
   {
      var visitedExpression = _instance.Visit(expression);

      if (visitedExpression is T lambda)
         return lambda;

      throw new NotSupportedException($"The provided expression is not supported: {expression}");
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression node)
   {
      if (node == null)
         throw new ArgumentNullException(nameof(node));

      if (!node.Method.IsGenericMethod || node.Method.GetGenericMethodDefinition() != _extractBodyMethod)
         return base.VisitMethodCall(node);

      var newParameter = node.Arguments[1];
      var lambda = LambdaExpressionSearchingVisitor.GetLambda(node.Arguments[0]);
      var oldParameter = lambda.Parameters[0];

      var expressionReplacer = new ExpressionReplacingVisitor(oldParameter, newParameter);
      var expression = expressionReplacer.Visit(lambda.Body);

      return Visit(expression);
   }
}
