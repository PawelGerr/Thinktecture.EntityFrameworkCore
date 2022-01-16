using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class EntityNameProvider
{
   /// <summary>
   /// This is an internal API.
   /// </summary>
   public static string GetCollectionParameterName(Type type, bool isScalar)
   {
      return $"Thinktecture:{(isScalar ? "Scalar" : "Complex")}CollectionParameter:{type.Namespace}.{type.ShortDisplayName()}";
   }

   /// <summary>
   /// This is an internal API.
   /// </summary>
   public static string GetTempTableName(Type type)
   {
      return $"Thinktecture:TempTable:{type.Namespace}.{type.ShortDisplayName()}";
   }
}
