using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
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
      /// <param name="queryCompilationContext"></param>
      /// <param name="tempTableQueryContextFactory"></param>
      /// <returns>Translated method call if a custom method is found; otherwise <c>null</c>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="visitor"/> or <paramref name="methodCallExpression"/> is <c>null</c>.
      /// </exception>
      public static Expression? TranslateBulkMethods(
         this RelationalQueryableMethodTranslatingExpressionVisitor visitor,
         MethodCallExpression methodCallExpression,
         IRelationalTypeMappingSource typeMappingSource,
         QueryCompilationContext queryCompilationContext,
         TempTableQueryContextFactory tempTableQueryContextFactory)
      {
         if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));
         if (methodCallExpression == null)
            throw new ArgumentNullException(nameof(methodCallExpression));

         if (methodCallExpression.Method.DeclaringType == typeof(BulkOperationsQueryableExtensions))
         {
            if (methodCallExpression.Method.Name == nameof(BulkOperationsQueryableExtensions.BulkDelete))
               return TranslateBulkDelete(GetShapedQueryExpression(visitor, methodCallExpression), typeMappingSource);

            throw new InvalidOperationException(CoreStrings.TranslationFailed(methodCallExpression.Print()));
         }

         if (methodCallExpression.Method.DeclaringType == typeof(BulkOperationsDbSetExtensions))
         {
            if (methodCallExpression.Method.Name == nameof(BulkOperationsDbSetExtensions.FromTempTable))
               return TranslateFromTempTable(GetShapedQueryExpression(visitor, methodCallExpression), methodCallExpression, queryCompilationContext, tempTableQueryContextFactory);

            throw new InvalidOperationException(CoreStrings.TranslationFailed(methodCallExpression.Print()));
         }

         return null;
      }

      private static Expression TranslateFromTempTable(
         ShapedQueryExpression shapedQueryExpression,
         MethodCallExpression methodCallExpression,
         QueryCompilationContext queryCompilationContext,
         TempTableQueryContextFactory tempTableQueryContextFactory)
      {
         var tableExpression = (TableExpression)((SelectExpression)shapedQueryExpression.QueryExpression).Tables.Single();
         var tempTableName = ((TempTableNameExpression)methodCallExpression.Arguments[1]).Value;

         var ctx = tempTableQueryContextFactory.Create(tableExpression, tempTableName ?? throw new Exception("No temp table name provided."));
         var extractor = Expression.Lambda<Func<QueryContext, TempTableQueryContext>>(Expression.Constant(ctx), QueryCompilationContext.QueryContextParameter);

         queryCompilationContext.RegisterRuntimeParameter(ctx.ParameterName, extractor);

         return shapedQueryExpression;
      }

      private static Expression TranslateBulkDelete(ShapedQueryExpression shapedQueryExpression, IRelationalTypeMappingSource typeMappingSource)
      {
         var selectExpression = (SelectExpression)shapedQueryExpression.QueryExpression;
         var intTypeMapping = typeMappingSource.FindMapping(typeof(int)) ?? throw new Exception($"The type mapping source '{typeMappingSource.GetType().Name}' has no mapping for 'int'.");

#pragma warning disable EF1001
         var clone = selectExpression.Clone();
#pragma warning restore EF1001
         clone.ApplyProjection(shapedQueryExpression.ShaperExpression, shapedQueryExpression.ResultCardinality, QuerySplittingBehavior.SingleQuery);
         var tableToDeleteIn = GetTableForDeleteOperation(clone);

         var projectionMapping = new Dictionary<ProjectionMember, Expression> { { new ProjectionMember(), new DeleteExpression(tableToDeleteIn, intTypeMapping) } };
         selectExpression.ReplaceProjection(Array.Empty<Expression>());
         selectExpression.ReplaceProjection(projectionMapping);

         var conversionToInt = Expression.Convert(new ProjectionBindingExpression(shapedQueryExpression.QueryExpression, new ProjectionMember(), typeof(int)), typeof(int));

         return shapedQueryExpression.UpdateResultCardinality(ResultCardinality.Single)
                                     .UpdateShaperExpression(conversionToInt);
      }

      private static TableExpressionBase GetTableForDeleteOperation(SelectExpression selectExpression)
      {
         TableExpressionBase? table = null;

         if (selectExpression.Projection.Count == 0 && selectExpression.Tables.Count == 1)
            return selectExpression.Tables[0];

         foreach (ProjectionExpression projection in selectExpression.Projection)
         {
            if (projection.Expression is not ColumnExpression columnExpression)
               throw new NotSupportedException($"The projection '{projection.Print()}' was expected to be a column but is a '{projection.Expression.GetType().Name}'.");

            if (table is null)
            {
               table = columnExpression.Table;
            }
            else
            {
               if (!table.Equals(columnExpression.Table))
                  throw new NotSupportedException($"The provided query is referencing more than 1 table. Found tables: [{GetDisplayName(table)}, {GetDisplayName(columnExpression.Table)}].");
            }
         }

         if (table == null)
            throw new NotSupportedException("A DELETE statement is not supported if the table has no columns.");

         if (table is JoinExpressionBase join)
            return join.Table;

         return table;
      }

      private static string GetDisplayName(TableExpressionBase table)
      {
         if (table is TableExpression realTable)
            return $"{realTable.Name} AS {realTable.Alias}";

         if (table is JoinExpressionBase join)
            return GetDisplayName(join.Table);

         return table.Type.Name;
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
