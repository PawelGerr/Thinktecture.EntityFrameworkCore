using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microting.EntityFrameworkCore.MySql.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the MySQL nullability processor with Thinktecture SQL expressions.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlSqlNullabilityProcessor : MySqlSqlNullabilityProcessor
{
   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlSqlNullabilityProcessor" />.
   /// </summary>
   /// <param name="dependencies">Processor dependencies.</param>
   /// <param name="parameters">Processor parameters.</param>
   public ThinktectureMySqlSqlNullabilityProcessor(
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
            nullable = false;
            return sqlExpression;
         case WindowFunctionExpression:
            nullable = true;
            return sqlExpression;
         default:
            return base.VisitCustomSqlExpression(sqlExpression, allowOptimizedExpansion, out nullable);
      }
   }
}
