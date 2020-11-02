using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the capabilities of <see cref="Microsoft.EntityFrameworkCore.Sqlite.Query.Internal.SqliteQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor
      : SqliteQueryableMethodTranslatingExpressionVisitor
   {
      /// <inheritdoc />
      public ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor(QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                         RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
                                                                         IModel model)
         : base(dependencies, relationalDependencies, model)
      {
      }

      /// <inheritdoc />
      protected ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor(ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor parentVisitor)
         : base(parentVisitor)
      {
      }

      /// <inheritdoc />
      protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
      {
         return new ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor(this);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TranslateCustomMethods(methodCallExpression) ?? base.VisitMethodCall(methodCallExpression);
      }
   }
}
