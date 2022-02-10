using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
public class ThinktectureSqlServerParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

   /// <summary>
   /// Initializes <see cref="ThinktectureSqlServerParameterBasedSqlProcessorFactory"/>.
   /// </summary>
   /// <param name="relationalOptimizingVisitor">Optimizing visitor.</param>
   /// <param name="dependencies">Dependencies.</param>
   public ThinktectureSqlServerParameterBasedSqlProcessorFactory(
      RelationalOptimizingVisitor relationalOptimizingVisitor,
      RelationalParameterBasedSqlProcessorDependencies dependencies)
   {
      _relationalOptimizingVisitor = relationalOptimizingVisitor;
      _dependencies = dependencies;
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
   {
      return new ThinktectureSqlServerParameterBasedSqlProcessor(_relationalOptimizingVisitor, _dependencies, useRelationalNulls);
   }
}
