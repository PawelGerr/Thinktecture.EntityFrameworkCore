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
      RelationalParameterBasedSqlProcessorParameters parameters)
      : base(dependencies, parameters)
   {
   }

   /// <inheritdoc />
   protected override SqlExpression VisitCustomSqlExpression(SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
   {
      switch (sqlExpression)
      {
         case INotNullableSqlExpression:
         {
            nullable = false;
            return sqlExpression;
         }
         case WindowFunctionExpression:
         {
            nullable = true;
            return sqlExpression;
         }
         default:
            return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
      }
   }
}
