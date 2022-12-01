using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture;

internal static class SqlServerCollectionExtensions
{
   public static void ComputeHashCode(this IEnumerable<PropertyWithNavigations> properties, HashCode hashCode)
   {
      foreach (var property in properties)
      {
         hashCode.Add(property);
      }
   }

   public static void ComputeHashCode(this IEnumerable<IProperty> properties, HashCode hashCode)
   {
      foreach (var property in properties)
      {
         hashCode.Add(property);
      }
   }

   public static bool AreEqual(this IReadOnlyCollection<PropertyWithNavigations> collection, IReadOnlyCollection<PropertyWithNavigations> other)
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
