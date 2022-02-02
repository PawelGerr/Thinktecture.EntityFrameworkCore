using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

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
   protected override TableExpressionBase Visit(TableExpressionBase tableExpressionBase)
   {
      if (tableExpressionBase is INotNullableSqlExpression)
         return tableExpressionBase;

      return base.Visit(tableExpressionBase);
   }

   /// <inheritdoc />
   protected override SqlExpression VisitCustomSqlExpression(SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
   {
      if (sqlExpression is INotNullableSqlExpression)
      {
         nullable = false;
         return sqlExpression;
      }

      return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
   }
}
