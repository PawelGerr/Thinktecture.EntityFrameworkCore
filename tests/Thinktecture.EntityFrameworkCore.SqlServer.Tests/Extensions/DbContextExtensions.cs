using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

public static class DbContextExtensions
{
   public static IEntityType GetEntityType<T>(this DbContext ctx)
   {
      return ctx.Model.GetEntityType(typeof(T));
   }

   public static IEntityType GetTempTableEntityType<T>(this DbContext ctx)
   {
      var name = EntityNameProvider.GetTempTableName(typeof(T));

      return ctx.Model.GetEntityType(name, typeof(T));
   }
}
