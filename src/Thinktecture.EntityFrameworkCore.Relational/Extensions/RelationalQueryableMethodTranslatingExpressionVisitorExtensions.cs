using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
public static class RelationalQueryableMethodTranslatingExpressionVisitorExtensions
{
   /// <summary>
   /// Translates custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   /// <param name="visitor">The visitor.</param>
   /// <param name="methodCallExpression">Method call to translate.</param>
   /// <returns>Translated method call if a custom method is found; otherwise <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="visitor"/> or <paramref name="methodCallExpression"/> is <c>null</c>.
   /// </exception>
   public static Expression? TranslateRelationalMethods(
      this RelationalQueryableMethodTranslatingExpressionVisitor visitor,
      MethodCallExpression methodCallExpression)
   {
      ArgumentNullException.ThrowIfNull(visitor);
      ArgumentNullException.ThrowIfNull(methodCallExpression);

      if (methodCallExpression.Method.DeclaringType == typeof(RelationalQueryableExtensions))
      {
         if (methodCallExpression.Method.Name == nameof(RelationalQueryableExtensions.AsSubQuery))
         {
            var expression = visitor.Visit(methodCallExpression.Arguments[0]);

            if (expression is ShapedQueryExpression shapedQueryExpression)
            {
               ((SelectExpression)shapedQueryExpression.QueryExpression).PushdownIntoSubquery();
               return shapedQueryExpression;
            }
         }

         if (methodCallExpression.Method.Name == nameof(RelationalQueryableExtensions.WithTableHints))
            return TranslateTableHints(GetShapedQueryExpression(visitor, methodCallExpression), methodCallExpression);
      }

      return null;
   }

   private static Expression TranslateTableHints(
      ShapedQueryExpression shapedQueryExpression,
      MethodCallExpression methodCallExpression)
   {
      var hintArgs = methodCallExpression.Arguments[1];
      var tableHints = hintArgs switch
      {
         TableHintsExpression tableHintsExpression => tableHintsExpression.Value,
         ConstantExpression constantExpression => (IReadOnlyList<ITableHint>?)constantExpression.Value ?? throw new Exception("No table hints provided."),
         _ => throw new NotSupportedException($"Table hint argument of type '{hintArgs.GetType().FullName}' is not supported.")
      };

      if (tableHints.Count == 0)
         return shapedQueryExpression;

      var selectExpression = (SelectExpression)shapedQueryExpression.QueryExpression;
      var newSelectExpression = selectExpression.AddAnnotation(new Annotation(ThinktectureRelationalAnnotationNames.TABLE_HINTS, tableHints));

      return shapedQueryExpression.Update(newSelectExpression, shapedQueryExpression.ShaperExpression);
   }

   private static ShapedQueryExpression GetShapedQueryExpression(
      ExpressionVisitor visitor,
      MethodCallExpression methodCallExpression)
   {
      var source = visitor.Visit(methodCallExpression.Arguments[0]);

      if (source is not ShapedQueryExpression shapedQueryExpression)
         throw new InvalidOperationException(CoreStrings.TranslationFailed(methodCallExpression.Print()));

      return shapedQueryExpression;
   }
}
