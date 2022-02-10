using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessor : SqlServerParameterBasedSqlProcessor
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;

   /// <inheritdoc />
   public ThinktectureSqlServerParameterBasedSqlProcessor(
      RelationalOptimizingVisitor relationalOptimizingVisitor,
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      bool useRelationalNulls)
      : base(dependencies, useRelationalNulls)
   {
      _relationalOptimizingVisitor = relationalOptimizingVisitor;
   }

   /// <inheritdoc />
   protected override SelectExpression ProcessSqlNullability(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      ArgumentNullException.ThrowIfNull(selectExpression);
      ArgumentNullException.ThrowIfNull(parametersValues);

      return new ThinktectureSqlServerSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(selectExpression, parametersValues, out canCache);
   }

   /// <inheritdoc />
   public override SelectExpression Optimize(SelectExpression selectExpression, IReadOnlyDictionary<string, object?> parametersValues, out bool canCache)
   {
      selectExpression = base.Optimize(selectExpression, parametersValues, out canCache);

      return _relationalOptimizingVisitor.Process(selectExpression);
   }
}
