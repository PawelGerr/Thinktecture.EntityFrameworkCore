using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
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
      /// <param name="queryCompilationContext"></param>
      /// <param name="tableHintContextFactory">Factory for <see cref="TableHintContext"/>.</param>
      /// <returns>Translated method call if a custom method is found; otherwise <c>null</c>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="visitor"/> or <paramref name="methodCallExpression"/> is <c>null</c>.
      /// </exception>
      public static Expression? TranslateRelationalMethods(
         this RelationalQueryableMethodTranslatingExpressionVisitor visitor,
         MethodCallExpression methodCallExpression,
         QueryCompilationContext queryCompilationContext,
         TableHintContextFactory tableHintContextFactory)
      {
         if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));
         if (methodCallExpression == null)
            throw new ArgumentNullException(nameof(methodCallExpression));

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
               return TranslateTableHints(GetShapedQueryExpression(visitor, methodCallExpression), methodCallExpression, queryCompilationContext, tableHintContextFactory);
         }

         return null;
      }

      private static Expression TranslateTableHints(
         ShapedQueryExpression shapedQueryExpression,
         MethodCallExpression methodCallExpression,
         QueryCompilationContext queryCompilationContext,
         TableHintContextFactory tableHintContextFactory)
      {
         var tableHints = (NonEvaluatableConstantExpression)methodCallExpression.Arguments[1] ?? throw new InvalidOperationException("Table hints cannot be null.");
         var tables = ((SelectExpression)shapedQueryExpression.QueryExpression).Tables;

         if (tables.Count == 0)
            throw new InvalidOperationException($"No tables found to apply table hints '{String.Join(", ", tableHints)}' to.");

         if (tables.Count > 1)
            throw new InvalidOperationException($"Multiple tables found to apply table hints '{String.Join(", ", tableHints)}' to. Expression: {String.Join(", ", tables.Select(t => t.Print()))}");

         var tableExpression = ((SelectExpression)shapedQueryExpression.QueryExpression).Tables[0];

         var ctx = tableHintContextFactory.Create(tableExpression, (IReadOnlyList<ITableHint>)(tableHints.Value ?? throw new Exception("No table hints provided.")));
         var extractor = Expression.Lambda<Func<QueryContext, TableHintContext>>(Expression.Constant(ctx), QueryCompilationContext.QueryContextParameter);

         queryCompilationContext.RegisterRuntimeParameter(ctx.ParameterName, extractor);

         return shapedQueryExpression;
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
}
