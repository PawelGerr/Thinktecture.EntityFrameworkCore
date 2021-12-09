using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture;

/// <summary>
/// Uses "DroppedNavigations" only.
/// </summary>
internal class DroppedInlineNavigationsComparer
   : IEqualityComparer<(IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property)>
{
   public static readonly DroppedInlineNavigationsComparer Instance = new();

   public bool Equals(
      (IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) x,
      (IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) y)
   {
      if (x.DroppedNavigations.Count != y.DroppedNavigations.Count)
         return false;

      for (var i = 0; i < x.DroppedNavigations.Count; i++)
      {
         if (!x.DroppedNavigations[i].Equals(y.DroppedNavigations[i]))
            return false;
      }

      return true;
   }

   public int GetHashCode((IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) obj)
   {
      var hashCode = new HashCode();

      foreach (var navigation in obj.DroppedNavigations)
      {
         hashCode.Add(navigation);
      }

      return hashCode.ToHashCode();
   }
}
