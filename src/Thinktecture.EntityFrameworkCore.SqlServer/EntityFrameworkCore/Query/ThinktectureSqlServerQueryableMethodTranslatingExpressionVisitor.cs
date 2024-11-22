using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the capabilities of <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor
   : SqlServerQueryableMethodTranslatingExpressionVisitor
{
   /// <inheritdoc />
   public ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      SqlServerQueryCompilationContext queryCompilationContext,
      ISqlServerSingletonOptions sqlServerSingletonOptions)
      : base(dependencies, relationalDependencies, queryCompilationContext, sqlServerSingletonOptions)
   {
   }

   /// <inheritdoc />
   protected ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(
      ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor parentVisitor)
      : base(parentVisitor)
   {
   }

   /// <inheritdoc />
   protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
   {
      return new ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(this);
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
   {
      return this.TranslateRelationalMethods(methodCallExpression) ??
             this.TranslateBulkMethods(methodCallExpression) ??
             base.VisitMethodCall(methodCallExpression);
   }
}
