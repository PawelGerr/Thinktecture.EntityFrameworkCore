using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("ReSharper", "EF1001")]
public class ThinktectureSqlServerParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

   /// <summary>
   /// Initializes <see cref="ThinktectureSqlServerParameterBasedSqlProcessorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   public ThinktectureSqlServerParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
   {
      _dependencies = dependencies;
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
   {
      return new ThinktectureSqlServerParameterBasedSqlProcessor(_dependencies, useRelationalNulls);
   }
}