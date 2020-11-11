using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the built-in <see cref="Microsoft.EntityFrameworkCore.SqlServer.Query.Internal.SqlServerSqlTranslatingExpressionVisitor"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqlServerSqlTranslatingExpressionVisitor
      : SqlServerSqlTranslatingExpressionVisitor
   {
      /// <inheritdoc />
      public ThinktectureSqlServerSqlTranslatingExpressionVisitor(
         RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
         QueryCompilationContext queryCompilationContext,
         QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
         : base(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
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
