using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   public class ThinktectureRelationalParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
   {
      private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

      /// <summary>
      /// Initializes new instance of <see cref="ThinktectureRelationalParameterBasedSqlProcessorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      public ThinktectureRelationalParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
      {
         _dependencies = dependencies;
      }

      /// <inheritdoc />
      public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
      {
         return new ThinktectureRelationalParameterBasedSqlProcessor(_dependencies, useRelationalNulls);
      }
   }
}
