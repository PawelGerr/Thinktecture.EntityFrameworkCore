using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Thinktecture.EntityFrameworkCore.Conventions;

/// <summary>
/// Convention not found in current <see cref="ConventionSet"/>.
/// </summary>
public class ConventionNotFoundException : Exception
{
   internal ConventionNotFoundException(ConventionToRemove conventionToRemove)
      : base($"Could not remove the convention with the implementation type '{conventionToRemove.ImplementationTypeToRemove.ShortDisplayName()}' because it wasn't in the collection '{conventionToRemove.ConventionType.CollectionName}' of the current convention set.")
   {
   }
}
