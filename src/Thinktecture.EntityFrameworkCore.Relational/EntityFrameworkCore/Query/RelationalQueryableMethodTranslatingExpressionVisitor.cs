using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the capabilities of <see cref="Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public class RelationalQueryableMethodTranslatingExpressionVisitor : Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor
   {
      /// <inheritdoc />
      public RelationalQueryableMethodTranslatingExpressionVisitor(QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                   RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
                                                                   IModel model)
         : base(dependencies, relationalDependencies, model)
      {
      }

      /// <inheritdoc />
      protected RelationalQueryableMethodTranslatingExpressionVisitor(RelationalQueryableMethodTranslatingExpressionVisitor parentVisitor)
         : base(parentVisitor)
      {
      }

      /// <inheritdoc />
      [NotNull]
      protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
      {
         return new RelationalQueryableMethodTranslatingExpressionVisitor(this);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TranslateCustomMethods(methodCallExpression) ?? base.VisitMethodCall(methodCallExpression);
      }
   }
}
