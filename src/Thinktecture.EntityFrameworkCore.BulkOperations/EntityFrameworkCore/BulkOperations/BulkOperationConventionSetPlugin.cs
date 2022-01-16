using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Conventions for bulk operations.
/// </summary>
public class BulkOperationConventionSetPlugin : IConventionSetPlugin
{
   /// <inheritdoc />
   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      conventionSet.ModelInitializedConventions.Add(TempTableConvention.Instance);

      return conventionSet;
   }
}
