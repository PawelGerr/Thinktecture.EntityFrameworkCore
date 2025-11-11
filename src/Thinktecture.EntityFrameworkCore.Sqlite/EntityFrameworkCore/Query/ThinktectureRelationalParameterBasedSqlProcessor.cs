using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class ThinktectureSqliteParameterBasedSqlProcessor : SqliteParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureSqliteParameterBasedSqlProcessor(
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

      return new ThinktectureSqlNullabilityProcessor(Dependencies, Parameters).Process(selectExpression, decorator);
   }
}
