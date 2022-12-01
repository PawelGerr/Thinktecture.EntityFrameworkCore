using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture;

public static class RelationalCollectionExtensions
{
   public static void ComputeHashCode(this IEnumerable<INavigation> navigations, HashCode hashCode)
   {
      foreach (var navigation in navigations)
      {
         hashCode.Add(navigation);
      }
   }
}
