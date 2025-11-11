using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessor : SqlServerParameterBasedSqlProcessor
{
   private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;

   /// <inheritdoc />
   public ThinktectureSqlServerParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      RelationalParameterBasedSqlProcessorParameters parameters,
      ISqlServerSingletonOptions sqlServerSingletonOptions)
      : base(dependencies, parameters, sqlServerSingletonOptions)
   {
      _sqlServerSingletonOptions = sqlServerSingletonOptions;
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression selectExpression, ParametersCacheDecorator decorator)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(decorator);

      return new ThinktectureSqlServerSqlNullabilityProcessor(Dependencies, Parameters, _sqlServerSingletonOptions).Process(selectExpression, decorator);
   }
}
