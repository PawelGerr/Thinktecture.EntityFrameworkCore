using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal sealed class MySqlCollectionParameterConventionSetPlugin : IConventionSetPlugin
{
   private readonly MySqlDbContextOptionsExtensionOptions _options;

   public MySqlCollectionParameterConventionSetPlugin(MySqlDbContextOptionsExtensionOptions options)
   {
      _options = options ?? throw new ArgumentNullException(nameof(options));
   }

   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      if (_options.ConfigureCollectionParametersForPrimitiveTypes)
         conventionSet.ModelInitializedConventions.Add(MySqlCollectionParameterConvention.Instance);

      return conventionSet;
   }
}
