using System.Diagnostics.CodeAnalysis;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Creates MySQL SQL generators with Thinktecture support.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
   private readonly QuerySqlGeneratorDependencies _dependencies;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly IMySqlOptions _mySqlOptions;

   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlQuerySqlGeneratorFactory" />.
   /// </summary>
   /// <param name="dependencies">Generator dependencies.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   public ThinktectureMySqlQuerySqlGeneratorFactory(
      QuerySqlGeneratorDependencies dependencies,
      IRelationalTypeMappingSource typeMappingSource,
      IMySqlOptions mySqlOptions)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
      _mySqlOptions = mySqlOptions ?? throw new ArgumentNullException(nameof(mySqlOptions));
   }

   /// <inheritdoc />
   public QuerySqlGenerator Create()
   {
      return new ThinktectureMySqlQuerySqlGenerator(_dependencies, _typeMappingSource, _mySqlOptions);
   }
}