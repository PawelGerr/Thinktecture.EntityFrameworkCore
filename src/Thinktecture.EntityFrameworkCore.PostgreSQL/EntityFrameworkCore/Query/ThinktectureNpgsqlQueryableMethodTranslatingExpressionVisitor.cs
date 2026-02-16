using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="NpgsqlQueryableMethodTranslatingExpressionVisitor"/> to support Thinktecture extensions.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor
   : NpgsqlQueryableMethodTranslatingExpressionVisitor
{
   /// <inheritdoc />
   public ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      RelationalQueryCompilationContext queryCompilationContext,
      INpgsqlSingletonOptions npgsqlSingletonOptions)
      : base(dependencies, relationalDependencies, queryCompilationContext, npgsqlSingletonOptions)
   {
   }

   /// <inheritdoc />
   protected ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor(
      ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor parentVisitor)
      : base(parentVisitor)
   {
   }

   /// <inheritdoc />
   protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
   {
      return new ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor(this);
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
   {
      return this.TranslateRelationalMethods(methodCallExpression) ??
             this.TranslateBulkMethods(methodCallExpression) ??
             base.VisitMethodCall(methodCallExpression);
   }
}
