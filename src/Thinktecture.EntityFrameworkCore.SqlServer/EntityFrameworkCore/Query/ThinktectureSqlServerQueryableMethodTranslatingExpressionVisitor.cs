using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the capabilities of <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor
      : RelationalQueryableMethodTranslatingExpressionVisitor
   {
      private readonly IRelationalTypeMappingSource _typeMappingSource;

      /// <inheritdoc />
      public ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(
         QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
         RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
         QueryCompilationContext queryCompilationContext,
         IRelationalTypeMappingSource typeMappingSource)
         : base(dependencies, relationalDependencies, queryCompilationContext)
      {
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      }

      /// <inheritdoc />
      protected ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(
         ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor parentVisitor,
         IRelationalTypeMappingSource typeMappingSource)
         : base(parentVisitor)
      {
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      }

      /// <inheritdoc />
      protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
      {
         return new ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(this, _typeMappingSource);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TranslateRelationalMethods(methodCallExpression, QueryCompilationContext) ??
                this.TranslateBulkMethods(methodCallExpression, _typeMappingSource, QueryCompilationContext) ??
                base.VisitMethodCall(methodCallExpression);
      }
   }
}
