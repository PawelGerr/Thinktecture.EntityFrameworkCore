using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture;

/// <summary>
/// For internal use only.
/// </summary>
public static class RelationalSelectExpressionExtensions
{
   /// <summary>
   /// For internal use only.
   /// </summary>
   public static SelectExpression AddAnnotation(
      this SelectExpression selectExpression,
      IAnnotation annotation)
   {
        ArgumentNullException.ThrowIfNull(selectExpression);
        ArgumentNullException.ThrowIfNull(annotation);

        var tables = selectExpression.Tables;

        if (tables.Count == 0)
           throw new InvalidOperationException($"No tables found to add annotation '{annotation.Name}' to.");

        foreach (var table in tables)
        {
            if (table is not TableExpression && table is not LeftJoinExpression && table is not CrossJoinExpression && table is not InnerJoinExpression)
            {
                throw new NotSupportedException($"Annotation '{annotation.Name}' can be applied to tables only but found '{table.GetType().Name}'. Expression: {table.Print()}");
            }
        }
        return (SelectExpression)new AnnotationApplyingExpressionVisitor(annotation)
         .Visit(selectExpression)!;
   }

   private sealed class AnnotationApplyingExpressionVisitor : ExpressionVisitor
   {
        private readonly IAnnotation _annotation;

        public AnnotationApplyingExpressionVisitor(IAnnotation annotation)
        {
           _annotation = annotation;
        }
#nullable enable
        public override Expression? Visit(Expression? expression)
        {
            return base.Visit(expression switch
            {
                LeftJoinExpression => (expression as LeftJoinExpression)?.AddAnnotation(_annotation.Name, _annotation.Value),
                InnerJoinExpression => (expression as InnerJoinExpression)?.AddAnnotation(_annotation.Name, _annotation.Value),
                CrossJoinExpression => (expression as CrossJoinExpression)?.AddAnnotation(_annotation.Name, _annotation.Value),
                TableExpression => (expression as TableExpression)?.AddAnnotation(_annotation.Name, _annotation.Value),
                _ => expression
            });
        }
#nullable disable
    }
}
