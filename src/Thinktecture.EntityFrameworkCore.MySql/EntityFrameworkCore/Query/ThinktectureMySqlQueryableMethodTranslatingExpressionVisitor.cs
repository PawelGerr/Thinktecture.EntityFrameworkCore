using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the MySQL queryable method translator with Thinktecture relational and bulk methods.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor : MySqlQueryableMethodTranslatingExpressionVisitor
{
   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor" />.
   /// </summary>
   /// <param name="dependencies">Queryable translation dependencies.</param>
   /// <param name="relationalDependencies">Relational translation dependencies.</param>
   /// <param name="queryCompilationContext">The query compilation context.</param>
   /// <param name="mySqlOptions">MySQL options.</param>
   public ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      RelationalQueryCompilationContext queryCompilationContext,
      IMySqlOptions mySqlOptions)
      : base(dependencies, relationalDependencies, queryCompilationContext, mySqlOptions)
   {
   }

   private ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor(ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor parent)
      : base(parent)
   {
   }

   /// <inheritdoc />
   protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
   {
      return new ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor(this);
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
   {
      return this.TranslateRelationalMethods(methodCallExpression)
             ?? this.TranslateBulkMethods(methodCallExpression)
             ?? base.VisitMethodCall(methodCallExpression);
   }
}