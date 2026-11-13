using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the MySQL parameter-based SQL processor with Thinktecture expressions.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlParameterBasedSqlProcessor : MySqlParameterBasedSqlProcessor
{
   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlParameterBasedSqlProcessor" />.
   /// </summary>
   /// <param name="dependencies">Processor dependencies.</param>
   /// <param name="parameters">Processor parameters.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   public ThinktectureMySqlParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      RelationalParameterBasedSqlProcessorParameters parameters,
      IMySqlOptions mySqlOptions)
      : base(dependencies, parameters, mySqlOptions)
   {
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression selectExpression, ParametersCacheDecorator decorator)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(decorator);

      return new ThinktectureMySqlSqlNullabilityProcessor(Dependencies, Parameters).Process(selectExpression, decorator);
   }
}
