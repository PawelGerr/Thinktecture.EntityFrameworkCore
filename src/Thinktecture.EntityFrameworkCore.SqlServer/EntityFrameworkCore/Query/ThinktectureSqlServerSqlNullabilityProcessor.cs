using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
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
   public ThinktectureSqlServerSqlNullabilityProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      RelationalParameterBasedSqlProcessorParameters parameters,
      ISqlServerSingletonOptions sqlServerSingletonOptions)
      : base(dependencies, parameters, sqlServerSingletonOptions)
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
