using System.Diagnostics.CodeAnalysis;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Creates MySQL parameter-based SQL processors with Thinktecture support.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
   private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;
   private readonly IMySqlOptions _mySqlOptions;

   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlParameterBasedSqlProcessorFactory" />.
   /// </summary>
   /// <param name="dependencies">Processor dependencies.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   public ThinktectureMySqlParameterBasedSqlProcessorFactory(
      RelationalParameterBasedSqlProcessorDependencies dependencies,
      IMySqlOptions mySqlOptions)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _mySqlOptions = mySqlOptions ?? throw new ArgumentNullException(nameof(mySqlOptions));
   }

   /// <inheritdoc />
   public RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
   {
      return new ThinktectureMySqlParameterBasedSqlProcessor(_dependencies, parameters, _mySqlOptions);
   }
}
