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

      if (tables.Count > 1)
         throw new InvalidOperationException($"Multiple tables found to add annotation '{annotation.Name}' to. Expressions: {String.Join(", ", tables.Select(t => t.Print()))}");

      var tableExpressionBase = selectExpression.Tables[0];

      if (tableExpressionBase is not TableExpression tableExpression)
         throw new NotSupportedException($"Annotation '{annotation.Name}' can be applied to tables only but found '{tableExpressionBase.GetType().Name}'. Expression: {tableExpressionBase.Print()}");

      return (SelectExpression)new AnnotationApplyingExpressionVisitor(tableExpression, annotation)
         .Visit(selectExpression)!;
   }

   private sealed class AnnotationApplyingExpressionVisitor : ExpressionVisitor
   {
      private readonly TableExpression _tableExpression;
      private readonly IAnnotation _annotation;

      public AnnotationApplyingExpressionVisitor(TableExpression tableExpression, IAnnotation annotation)
      {
         _tableExpression = tableExpression;
         _annotation = annotation;
      }

      public override Expression? Visit(Expression? expression)
      {
         return _tableExpression.Equals(expression)
                   ? _tableExpression.AddAnnotation(_annotation.Name, _annotation.Value)
                   : base.Visit(expression);
      }
   }
}
