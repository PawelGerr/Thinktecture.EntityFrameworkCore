using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture;

internal static class NpgsqlCollectionExtensions
{
   public static void ComputeHashCode(this IEnumerable<IProperty> properties, HashCode hashCode)
   {
      foreach (var property in properties)
      {
         hashCode.Add(property);
      }
   }

   public static bool AreEqual(this IReadOnlyCollection<IProperty> collection, IReadOnlyCollection<IProperty> other)
   {
      if (collection.Count != other.Count)
         return false;

      using var otherEnumerator = other.GetEnumerator();

      foreach (var item in collection)
      {
         otherEnumerator.MoveNext();

         if (!item.Equals(otherEnumerator.Current))
            return false;
      }

      return true;
   }
}
