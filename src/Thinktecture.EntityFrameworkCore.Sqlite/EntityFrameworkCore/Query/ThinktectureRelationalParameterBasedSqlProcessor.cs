using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
/// </summary>
public class ThinktectureSqliteParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;

   /// <inheritdoc />
   public ThinktectureSqliteParameterBasedSqlProcessor(
      RelationalOptimizingVisitor relationalOptimizingVisitor,
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
      _relationalOptimizingVisitor = relationalOptimizingVisitor;
   }

   /// <inheritdoc />
   protected override Expression ProcessSqlNullability(Expression expression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(expression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override Expression Optimize(Expression expression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      expression = base.Optimize(expression, parametersValues, out canCache);

      return _relationalOptimizingVisitor.Process(expression);
   }
}
