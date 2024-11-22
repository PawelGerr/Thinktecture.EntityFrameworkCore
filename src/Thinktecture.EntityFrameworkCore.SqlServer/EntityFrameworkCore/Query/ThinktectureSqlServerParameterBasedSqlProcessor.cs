using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessor : SqlServerParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureSqlServerParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      RelationalParameterBasedSqlProcessorParameters parameters)
      : base(dependencies, parameters)
   {
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlServerSqlNullabilityProcessor(Dependencies, Parameters).Process(selectExpression, parametersValues, out canCache);
   }
}
