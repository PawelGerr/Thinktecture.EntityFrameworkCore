using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class ThinktectureSqlServerParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;
   private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;

   /// <summary>
   /// Initializes <see cref="ThinktectureSqlServerParameterBasedSqlProcessorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="sqlServerSingletonOptions">Options.</param>
   public ThinktectureSqlServerParameterBasedSqlProcessorFactory(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      ISqlServerSingletonOptions sqlServerSingletonOptions)
   {
      _dependencies = dependencies;
      _sqlServerSingletonOptions = sqlServerSingletonOptions;
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
   {
      return new ThinktectureSqlServerParameterBasedSqlProcessor(_dependencies, parameters, _sqlServerSingletonOptions);
   }
}
