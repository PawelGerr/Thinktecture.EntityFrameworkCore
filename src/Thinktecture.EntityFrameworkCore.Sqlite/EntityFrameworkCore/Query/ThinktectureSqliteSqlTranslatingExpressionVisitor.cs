using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends <see cref="SqliteSqlTranslatingExpressionVisitor"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqliteSqlTranslatingExpressionVisitor : SqliteSqlTranslatingExpressionVisitor
   {
      /// <inheritdoc />
      public ThinktectureSqliteSqlTranslatingExpressionVisitor(RelationalSqlTranslatingExpressionVisitorDependencies dependencies, IModel model, QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
         : base(dependencies, model, queryableMethodTranslatingExpressionVisitor)
      {
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TryTranslateCountDistinct(methodCallExpression, Dependencies.SqlExpressionFactory, out var countDistinctExpression)
                   ? countDistinctExpression
                   : base.VisitMethodCall(methodCallExpression);
      }
   }
}
