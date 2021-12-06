using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

public static class DbContextExtensions
{
   public static IEntityType GetEntityType<T>(this DbContext ctx)
   {
      return ctx.Model.GetEntityType(typeof(T));
   }
}