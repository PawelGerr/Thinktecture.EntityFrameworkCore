using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/> to support Thinktecture extensions.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureNpgsqlParameterBasedSqlProcessor : NpgsqlParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureNpgsqlParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      RelationalParameterBasedSqlProcessorParameters parameters)
      : base(dependencies, parameters)
   {
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression selectExpression, ParametersCacheDecorator decorator)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(decorator);

      return new ThinktectureNpgsqlSqlNullabilityProcessor(Dependencies, Parameters).Process(selectExpression, decorator);
   }
}
