using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
public class ThinktectureSqliteQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
   private readonly QuerySqlGeneratorDependencies _dependencies;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqliteQuerySqlGeneratorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   public ThinktectureSqliteQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
   }

   /// <inheritdoc />
   public QuerySqlGenerator Create()
   {
      return new ThinktectureSqliteQuerySqlGenerator(_dependencies);
   }
}
