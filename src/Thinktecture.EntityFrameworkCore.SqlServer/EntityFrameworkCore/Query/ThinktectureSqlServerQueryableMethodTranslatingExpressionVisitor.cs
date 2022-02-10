using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the capabilities of <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor
   : SqlServerQueryableMethodTranslatingExpressionVisitor
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
      return this.TranslateRelationalMethods(methodCallExpression) ??
             this.TranslateBulkMethods(methodCallExpression, _typeMappingSource, RelationalDependencies.SqlExpressionFactory) ??
             base.VisitMethodCall(methodCallExpression);
   }
}
