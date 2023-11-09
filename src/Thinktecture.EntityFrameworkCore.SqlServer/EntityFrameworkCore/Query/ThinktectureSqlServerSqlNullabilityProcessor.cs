using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="SqlServerSqlNullabilityProcessor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerSqlNullabilityProcessor : SqlServerSqlNullabilityProcessor
{
   /// <inheritdoc />
   public ThinktectureSqlServerSqlNullabilityProcessor(RelationalParameterBasedSqlProcessorDependencies dependencies, bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
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
         case WindowFunctionExpression { AggregateFunction: SqlServerAggregateFunctionExpression aggregateFunction } windowFunctionExpression:
         {
            var visitedAggregateFunction = base.VisitSqlServerAggregateFunction(aggregateFunction, allowOptimizedExpansion, out nullable);

            return aggregateFunction == visitedAggregateFunction
                      ? windowFunctionExpression
                      : new WindowFunctionExpression(visitedAggregateFunction, windowFunctionExpression.Partitions, windowFunctionExpression.Orderings);
         }
         default:
            return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
      }
   }
}
