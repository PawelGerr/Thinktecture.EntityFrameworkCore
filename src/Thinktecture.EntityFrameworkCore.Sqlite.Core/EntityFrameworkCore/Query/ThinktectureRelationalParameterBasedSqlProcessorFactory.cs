using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
public class ThinktectureSqliteParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqliteParameterBasedSqlProcessorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   public ThinktectureSqliteParameterBasedSqlProcessorFactory(
      RelationalParameterBasedSqlProcessorDependencies dependencies)
   {
      _dependencies = dependencies;
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
   {
      return new ThinktectureSqliteParameterBasedSqlProcessor(_dependencies, parameters);
   }
}
