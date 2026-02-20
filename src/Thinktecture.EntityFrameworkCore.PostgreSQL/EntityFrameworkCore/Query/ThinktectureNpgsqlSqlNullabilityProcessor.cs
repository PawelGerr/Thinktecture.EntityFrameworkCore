using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="NpgsqlSqlNullabilityProcessor"/> to support Thinktecture extensions.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureNpgsqlSqlNullabilityProcessor : NpgsqlSqlNullabilityProcessor
{
   /// <inheritdoc />
   public ThinktectureNpgsqlSqlNullabilityProcessor(
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
