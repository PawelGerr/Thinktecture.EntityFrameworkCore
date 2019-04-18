using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.Database;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class TestDbContextExtensions
   {
      public static async Task<Guid> EnsureCustomerAsync([NotNull] this TestDbContext ctx, Guid id)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         if (!await ctx.Customers.AnyAsync(c => c.Id == id))
         {
            ctx.Customers.Add(new Customer { Id = id });
            await ctx.SaveChangesAsync();
         }

         return id;
      }
   }
}
