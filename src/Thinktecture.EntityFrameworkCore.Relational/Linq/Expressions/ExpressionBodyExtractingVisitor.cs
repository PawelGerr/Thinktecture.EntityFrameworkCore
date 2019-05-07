using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Thinktecture.Linq.Expressions
{
   /// <summary>
   /// Searches for occurrences of <see cref="ExpressionExtensions.ExtractBody{TIn,TOut}"/>
   /// and replaces them with the body of the provided lambda expression.
   /// The references to the original lambda parameter is replaced with the one provided with <see cref="ExpressionExtensions.ExtractBody{TIn,TOut}"/>.
   /// </summary>
   public class ExpressionBodyExtractingVisitor : ExpressionVisitor
   {
      /// <summary>
      /// An instance of <see cref="ExpressionBodyExtractingVisitor"/>.
      /// </summary>
      public static readonly ExpressionBodyExtractingVisitor Instance = new ExpressionBodyExtractingVisitor();

      private static readonly MethodInfo _extractBodyMethod;

      static ExpressionBodyExtractingVisitor()
      {
         _extractBodyMethod = typeof(ExpressionExtensions).GetMethod(nameof(ExpressionExtensions.ExtractBody), BindingFlags.Static | BindingFlags.Public);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression node)
      {
         if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == _extractBodyMethod)
         {
            var newParameter = node.Arguments[1];
            var lambda = LambdaExpressionSearchingVisitor.Instance.GetLambda(node.Arguments[0]);
            var oldParameter = lambda.Parameters[0];

            var expressionReplacer = new ExpressionReplacingVisitor(oldParameter, newParameter);
            var expression = expressionReplacer.Visit(lambda.Body);

            return Visit(expression);
         }

         return base.VisitMethodCall(node);
      }
   }
}
