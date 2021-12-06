using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
/// </summary>
public class ThinktectureSqliteParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
{
   /// <inheritdoc />
   public ThinktectureSqliteParameterBasedSqlProcessor(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
   }

   /// <inheritdoc />
   protected override SelectExpression ProcessSqlNullability(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(selectExpression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override SelectExpression Optimize(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      selectExpression = base.Optimize(selectExpression, parametersValues, out canCache);

      if (TempTableQueryContext.TryGetTempTableContexts(parametersValues, out var ctxs))
         selectExpression = new BulkOperationOptimizingVisitor(parametersValues, ctxs, Array.Empty<TableHintContext>()).Process(selectExpression);

      return selectExpression;
   }
}
