using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the capabilities of <see cref="Microsoft.EntityFrameworkCore.Sqlite.Query.Internal.SqliteQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor : SqliteQueryableMethodTranslatingExpressionVisitor
{
   /// <inheritdoc />
   public ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      RelationalQueryCompilationContext queryCompilationContext)
      : base(dependencies, relationalDependencies, queryCompilationContext)
   {
   }

   /// <inheritdoc />
   protected ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(
      ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor parentVisitor)
      : base(parentVisitor)
   {
   }

   /// <inheritdoc />
   protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
   {
      return new ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(this);
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
   {
      return this.TranslateRelationalMethods(methodCallExpression) ??
             this.TranslateBulkMethods(methodCallExpression) ??
             base.VisitMethodCall(methodCallExpression);
   }
}
