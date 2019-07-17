using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class DbContextExtensions
   {
      [NotNull]
      public static IEntityType GetEntityType<T>([NotNull] this DbContext ctx)
      {
         return ctx.Model.GetEntityType(typeof(T));
      }
   }
}
