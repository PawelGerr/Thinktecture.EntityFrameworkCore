using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
/// </summary>
public class ThinktectureSqliteParameterBasedSqlProcessor : SqliteParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureSqliteParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression expression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(expression, parametersValues, out canCache);
   }
}
