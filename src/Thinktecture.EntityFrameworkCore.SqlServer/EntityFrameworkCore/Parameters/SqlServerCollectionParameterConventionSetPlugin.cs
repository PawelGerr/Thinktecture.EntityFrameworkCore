using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal class SqlServerCollectionParameterConventionSetPlugin : IConventionSetPlugin
{
   private readonly SqlServerDbContextOptionsExtensionOptions _options;

   public SqlServerCollectionParameterConventionSetPlugin(SqlServerDbContextOptionsExtensionOptions options)
   {
      _options = options ?? throw new ArgumentNullException(nameof(options));
   }

   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      if (_options.ConfigureCollectionParametersForPrimitiveTypes)
         conventionSet.ModelInitializedConventions.Add(SqlServerCollectionParameterConvention.Instance);

      return conventionSet;
   }
}
