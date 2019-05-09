using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Thinktecture.Database;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class DemoDbContextExtensions
   {
      public static async Task<Guid> EnsureCustomerAsync([NotNull] this DemoDbContext ctx, Guid id)
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

      public static async Task<Guid> EnsureProductAsync([NotNull] this DemoDbContext ctx, Guid id)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         if (!await ctx.Products.AnyAsync(c => c.Id == id))
         {
            ctx.Products.Add(new Product { Id = id });
            await ctx.SaveChangesAsync();
         }

         return id;
      }

      public static async Task<Guid> EnsureOrderAsync([NotNull] this DemoDbContext ctx, Guid id, Guid customerId)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         if (!await ctx.Orders.AnyAsync(c => c.Id == id))
         {
            ctx.Orders.Add(new Order { Id = id, CustomerId = customerId });
            await ctx.SaveChangesAsync();
         }

         return id;
      }

      public static async Task EnsureOrderItemAsync([NotNull] this DemoDbContext ctx, Guid orderId, Guid productId, int count)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         var orderItem = await ctx.OrderItems.FirstOrDefaultAsync(c => c.OrderId == orderId && c.ProductId == productId);

         if (orderItem == null)
         {
            orderItem = new OrderItem { OrderId = orderId, ProductId = productId };
            ctx.OrderItems.Add(orderItem);
         }

         orderItem.Count = count;
         await ctx.SaveChangesAsync();
      }
   }
}
