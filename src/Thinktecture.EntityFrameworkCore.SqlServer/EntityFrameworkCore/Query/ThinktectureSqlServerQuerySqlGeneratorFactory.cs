using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
public class ThinktectureSqlServerQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
   private readonly QuerySqlGeneratorDependencies _dependencies;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;
   private readonly ITenantDatabaseProviderFactory _databaseProviderFactory;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqlServerQuerySqlGeneratorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   /// <param name="sqlServerSingletonOptions">Options.</param>
   /// <param name="databaseProviderFactory">Factory.</param>
   public ThinktectureSqlServerQuerySqlGeneratorFactory(
      QuerySqlGeneratorDependencies dependencies,
      IRelationalTypeMappingSource typeMappingSource,
      ISqlServerSingletonOptions sqlServerSingletonOptions,
      ITenantDatabaseProviderFactory databaseProviderFactory)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _sqlServerSingletonOptions = sqlServerSingletonOptions;
      _databaseProviderFactory = databaseProviderFactory ?? throw new ArgumentNullException(nameof(databaseProviderFactory));
   }

   /// <inheritdoc />
   public QuerySqlGenerator Create()
   {
      return new ThinktectureSqlServerQuerySqlGenerator(_dependencies, _typeMappingSource, _sqlServerSingletonOptions, _databaseProviderFactory.Create());
   }
}
