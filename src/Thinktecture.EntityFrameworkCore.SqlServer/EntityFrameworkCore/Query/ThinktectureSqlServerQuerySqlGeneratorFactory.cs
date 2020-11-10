using System;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   public class ThinktectureSqlServerQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
   {
      private readonly QuerySqlGeneratorDependencies _dependencies;
      private readonly ITenantDatabaseProviderFactory _databaseProviderFactory;

      /// <summary>
      /// Initializes new instance of <see cref="ThinktectureSqlServerQuerySqlGeneratorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="databaseProviderFactory">Factory.</param>
      public ThinktectureSqlServerQuerySqlGeneratorFactory(
         QuerySqlGeneratorDependencies dependencies,
         ITenantDatabaseProviderFactory databaseProviderFactory)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
         _databaseProviderFactory = databaseProviderFactory ?? throw new ArgumentNullException(nameof(databaseProviderFactory));
      }

      /// <inheritdoc />
      public QuerySqlGenerator Create()
      {
         return new ThinktectureSqlServerQuerySqlGenerator(_dependencies, _databaseProviderFactory.Create());
      }
   }
}
