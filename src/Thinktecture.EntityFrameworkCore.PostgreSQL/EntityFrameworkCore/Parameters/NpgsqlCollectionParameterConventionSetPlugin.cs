using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal class NpgsqlCollectionParameterConventionSetPlugin : IConventionSetPlugin
{
   private readonly NpgsqlDbContextOptionsExtensionOptions _options;

   public NpgsqlCollectionParameterConventionSetPlugin(NpgsqlDbContextOptionsExtensionOptions options)
   {
      _options = options ?? throw new ArgumentNullException(nameof(options));
   }

   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      if (_options.ConfigureCollectionParametersForPrimitiveTypes)
         conventionSet.ModelInitializedConventions.Add(NpgsqlCollectionParameterConvention.Instance);

      return conventionSet;
   }
}
