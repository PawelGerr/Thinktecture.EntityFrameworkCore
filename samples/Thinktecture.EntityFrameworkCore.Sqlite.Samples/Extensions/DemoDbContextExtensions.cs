using Thinktecture.Database;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

public static class DemoDbContextExtensions
{
   public static async Task<Guid> EnsureCustomerAsync(this DemoDbContext ctx, Guid id)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      if (!await ctx.Customers.AnyAsync(c => c.Id == id))
      {
         ctx.Customers.Add(new Customer(id, $"First name of '{id}'", $"Last name of '{id}'"));
         await ctx.SaveChangesAsync();
      }

      return id;
   }

   public static async Task<Guid> EnsureProductAsync(this DemoDbContext ctx, Guid id)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      if (!await ctx.Products.AnyAsync(c => c.Id == id))
      {
         ctx.Products.Add(new Product { Id = id });
         await ctx.SaveChangesAsync();
      }

      return id;
   }

   public static async Task<Guid> EnsureOrderAsync(this DemoDbContext ctx, Guid id, Guid customerId)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      if (!await ctx.Orders.AnyAsync(c => c.Id == id))
      {
         ctx.Orders.Add(new Order { Id = id, CustomerId = customerId });
         await ctx.SaveChangesAsync();
      }

      return id;
   }

   public static async Task EnsureOrderItemAsync(this DemoDbContext ctx, Guid orderId, Guid productId, int count)
   {
      ArgumentNullException.ThrowIfNull(ctx);

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
