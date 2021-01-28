using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the capabilities of <see cref="Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public class ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
   {
      /// <inheritdoc />
      public ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor(
         QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
         RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
         QueryCompilationContext queryCompilationContext)
         : base(dependencies, relationalDependencies, queryCompilationContext)
      {
      }

      /// <inheritdoc />
      protected ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor(ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor parentVisitor)
         : base(parentVisitor)
      {
      }

      /// <inheritdoc />
      protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
      {
         return new ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor(this);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TranslateRelationalMethods(methodCallExpression) ?? base.VisitMethodCall(methodCallExpression);
      }
   }
}
