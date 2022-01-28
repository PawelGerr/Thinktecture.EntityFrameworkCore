using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Conventions;

internal class RelationalConventionSetPlugin : IConventionSetPlugin
{
   private readonly RelationalDbContextOptionsExtensionOptions _options;

   public RelationalConventionSetPlugin(RelationalDbContextOptionsExtensionOptions options)
   {
      _options = options;
   }

   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      foreach (var conventionToRemove in _options.ConventionsToRemove)
      {
         var successful = conventionToRemove.RemoveConvention(conventionSet);

         if (!successful && conventionToRemove.ThrowIfNotFound)
            throw new ConventionNotFoundException(conventionToRemove);
      }

      return conventionSet;
   }
}
