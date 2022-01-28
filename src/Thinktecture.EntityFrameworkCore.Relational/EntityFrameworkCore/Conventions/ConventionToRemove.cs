using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Thinktecture.EntityFrameworkCore.Conventions;

internal record ConventionToRemove(ConventionType ConventionType, Type ImplementationTypeToRemove, bool ThrowIfNotFound)
{
   public bool RemoveConvention(ConventionSet conventionSet)
   {
      return ConventionType.RemoveConvention(conventionSet, ImplementationTypeToRemove);
   }
}
