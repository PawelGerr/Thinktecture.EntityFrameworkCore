using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture;

/// <summary>
/// Extensions for collections.
/// </summary>
public static class RelationalCollectionExtensions
{
   /// <summary>
   /// Computes the hashcode for navigations.
   /// </summary>
   /// <param name="navigations">Navigations to compute hashcode for.</param>
   /// <param name="hashCode">Instance of <see cref="HashCode"/> to use.</param>
   public static void ComputeHashCode(this IEnumerable<INavigation> navigations, HashCode hashCode)
   {
      foreach (var navigation in navigations)
      {
         hashCode.Add(navigation);
      }
   }
}
