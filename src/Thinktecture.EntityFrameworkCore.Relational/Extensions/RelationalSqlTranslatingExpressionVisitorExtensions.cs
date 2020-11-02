using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="RelationalSqlTranslatingExpressionVisitor"/>.
   /// </summary>
   public static class RelationalSqlTranslatingExpressionVisitorExtensions
   {
      /// <summary>
      /// Translates the method call to <see cref="CountDistinctExpression"/> if applicable.
      /// </summary>
      /// <param name="visitor">Visitor.</param>
      /// <param name="methodCallExpression">Method call to translate.</param>
      /// <param name="sqlExpressionFactory">SQL expression factory.</param>
      /// <param name="translatedExpression">Translated expression.</param>
      /// <returns><c>true</c> if <paramref name="methodCallExpression"/> has been translated; otherwise <c>false</c>.</returns>
      /// <exception cref="InvalidOperationException">If the translation failed.</exception>
      public static bool TryTranslateCountDistinct(
         this RelationalSqlTranslatingExpressionVisitor visitor,
         MethodCallExpression methodCallExpression,
         ISqlExpressionFactory sqlExpressionFactory,
         [MaybeNullWhen(false)] out SqlExpression translatedExpression)
      {
         if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));
         if (sqlExpressionFactory == null)
            throw new ArgumentNullException(nameof(sqlExpressionFactory));

         if (methodCallExpression is null ||
             methodCallExpression.Method.DeclaringType != typeof(GroupingExtensions) ||
             nameof(GroupingExtensions.CountDistinct) != methodCallExpression.Method.Name)
         {
            translatedExpression = null;
            return false;
         }

         if (methodCallExpression.Arguments.Count != 2 ||
             !(methodCallExpression.Arguments[0] is GroupByShaperExpression groupByShaperExpression))
            throw new InvalidOperationException($"The method '{nameof(GroupingExtensions.CountDistinct)}' must be called with 2 arguments but found {methodCallExpression.Arguments.Count}).");

         var selector = GetSelector(methodCallExpression, groupByShaperExpression);
         var column = visitor.Translate(selector);

         if (column is null)
            throw new InvalidOperationException(CoreStrings.TranslationFailed(methodCallExpression.Arguments[1].Print()));

         translatedExpression = new CountDistinctExpression(column, sqlExpressionFactory.FindMapping(typeof(int)));
         return true;
      }

      private static Expression GetSelector(MethodCallExpression methodCallExpression, GroupByShaperExpression groupByShaperExpression)
      {
         var selectorLambda = UnwrapLambdaFromQuote(methodCallExpression.Arguments[1]);

         return ReplacingExpressionVisitor.Replace(selectorLambda.Parameters[0],
                                                   groupByShaperExpression.ElementSelector,
                                                   selectorLambda.Body);
      }

      private static LambdaExpression UnwrapLambdaFromQuote(Expression expression)
      {
         return (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                                      ? unary.Operand
                                      : expression);
      }
   }
}
