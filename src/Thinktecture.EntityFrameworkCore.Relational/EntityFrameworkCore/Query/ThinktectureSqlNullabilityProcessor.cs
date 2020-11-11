using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends <see cref="SqlNullabilityProcessor"/>.
   /// </summary>
   public class ThinktectureSqlNullabilityProcessor : SqlNullabilityProcessor
   {
      /// <inheritdoc />
      public ThinktectureSqlNullabilityProcessor(
         RelationalParameterBasedSqlProcessorDependencies dependencies,
         bool useRelationalNulls)
         : base(dependencies, useRelationalNulls)
      {
      }

      /// <inheritdoc />
      protected override SqlExpression VisitCustomSqlExpression(SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
      {
         if (sqlExpression is RowNumberClauseOrderingsExpression)
         {
            nullable = false;
            return sqlExpression;
         }

         if (sqlExpression is CountDistinctExpression countDistinctExpression)
         {
            var visitedExpression = Visit(countDistinctExpression.Column, allowOptimizedExpansion, out nullable);
            nullable = false;
            return countDistinctExpression.Update(visitedExpression);
         }

         return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
      }
   }
}
