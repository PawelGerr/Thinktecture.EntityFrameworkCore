using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Parameters;

internal class SqlServerCollectionParameterConventionSetPlugin : IConventionSetPlugin
{
   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      conventionSet.ModelInitializedConventions.Add(SqlServerCollectionParameterConvention.Instance);

      return conventionSet;
   }
}
