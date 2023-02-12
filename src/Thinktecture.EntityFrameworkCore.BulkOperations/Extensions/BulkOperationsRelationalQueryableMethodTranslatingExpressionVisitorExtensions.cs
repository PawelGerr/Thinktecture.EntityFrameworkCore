using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
public static class BulkOperationsRelationalQueryableMethodTranslatingExpressionVisitorExtensions
{
   /// <summary>
   /// Translates custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   /// <param name="visitor">The visitor.</param>
   /// <param name="methodCallExpression">Method call to translate.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   /// <param name="sqlExpressionFactory">SQL expression factory.</param>
   /// <returns>Translated method call if a custom method is found; otherwise <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="visitor"/> or <paramref name="methodCallExpression"/> is <c>null</c>.
   /// </exception>
   public static Expression? TranslateBulkMethods(
      this RelationalQueryableMethodTranslatingExpressionVisitor visitor,
      MethodCallExpression methodCallExpression,
      IRelationalTypeMappingSource typeMappingSource,
      ISqlExpressionFactory sqlExpressionFactory)
   {
      ArgumentNullException.ThrowIfNull(visitor);
      ArgumentNullException.ThrowIfNull(methodCallExpression);

      if (methodCallExpression.Method.DeclaringType == typeof(BulkOperationsDbSetExtensions))
      {
         if (methodCallExpression.Method.Name == nameof(BulkOperationsDbSetExtensions.FromTempTable))
         {
            var tempTableInfo = ((TempTableInfoExpression)methodCallExpression.Arguments[1]).Value;
            var shapedQueryExpression = GetShapedQueryExpression(visitor, methodCallExpression);

            return TranslateFromTempTable(shapedQueryExpression, tempTableInfo);
         }

         throw new InvalidOperationException(CoreStrings.TranslationFailed(methodCallExpression.Print()));
      }

      return null;
   }

   private static Expression TranslateFromTempTable(
      ShapedQueryExpression shapedQueryExpression,
      TempTableInfo tempTableInfo)
   {
      var tempTableName = tempTableInfo.Name ?? throw new Exception("No temp table name provided.");

      var selectExpression = (SelectExpression)shapedQueryExpression.QueryExpression;
      var newSelectExpression = selectExpression.AddAnnotation(new Annotation(ThinktectureBulkOperationsAnnotationNames.TEMP_TABLE, tempTableName));

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
