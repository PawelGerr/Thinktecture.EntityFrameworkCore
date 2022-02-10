using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
public class ThinktectureSqliteParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalOptimizingVisitor _relationalOptimizingVisitor;
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqliteParameterBasedSqlProcessorFactory"/>.
   /// </summary>
   /// <param name="relationalOptimizingVisitor">Optimizing visitor.</param>
   /// <param name="dependencies">Dependencies.</param>
   public ThinktectureSqliteParameterBasedSqlProcessorFactory(
      RelationalOptimizingVisitor relationalOptimizingVisitor,
      RelationalParameterBasedSqlProcessorDependencies dependencies)
   {
      _relationalOptimizingVisitor = relationalOptimizingVisitor;
      _dependencies = dependencies;
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
   {
      return new ThinktectureSqliteParameterBasedSqlProcessor(_relationalOptimizingVisitor, _dependencies, useRelationalNulls);
   }
}
