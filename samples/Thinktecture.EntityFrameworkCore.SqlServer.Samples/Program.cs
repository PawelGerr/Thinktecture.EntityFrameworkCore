using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;

[assembly: SuppressMessage("ReSharper", "CA2007")]
[assembly: SuppressMessage("ReSharper", "CA1052")]
[assembly: SuppressMessage("ReSharper", "CA2227")]

namespace Thinktecture
{
   // ReSharper disable once ClassNeverInstantiated.Global
   public class Program
   {
      // ReSharper disable once InconsistentNaming
      public static async Task Main(string[] args)
      {
         var sp = SamplesContext.Instance.CreateServiceProvider("demo");

         using (var scope = sp.CreateScope())
         {
            var ctx = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            await ctx.Database.MigrateAsync();

            var customerId = await ctx.EnsureCustomerAsync(new Guid("11D67C68-6F1A-407B-9BD3-56C84FE15BB1"));
            var productId = await ctx.EnsureProductAsync(new Guid("872BCAC2-1A85-4B22-AC0F-7D920563A000"));
            var orderId = await ctx.EnsureOrderAsync(new Guid("EC1CBF87-F53F-4EF4-B286-8F5EB0AE810D"), customerId);
            await ctx.EnsureOrderItemAsync(orderId, productId, 42);

            // Bulk insert into "real" tables
            await DoBulkInsertIntoRealTableAsync(ctx);
            await DoBulkInsertSpecifiedColumnsIntoRealTableAsync(ctx);

            // Bulk insert into temp tables
            await DoBulkInsertEntitiesIntoTempTableAsync(ctx);
            await DoBulkInsertIntoTempTableAsync(ctx, new List<Guid> { customerId });
            await DoBulkInsertIntoTempTableAsync(ctx, new List<(Guid, Guid)> { (customerId, productId) });

            // LEFT JOIN
            await DoLeftJoinAsync(ctx);

            // ROWNUMBER
            await DoRowNumberAsync(ctx);
         }

         Console.WriteLine("Exiting samples...");
      }

      private static async Task DoRowNumberAsync([NotNull] DemoDbContext ctx)
      {
         var customers = await ctx.Customers
                                  .Select(c => new
                                               {
                                                  c.Id,
                                                  RowNumber = EF.Functions.RowNumber(c.Id)
                                               })
                                  .ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customers.Select(c => $"{{ CustomerId={c.Id}, RowNumber={c.RowNumber} }}"))}");
      }

      private static async Task DoLeftJoinAsync([NotNull] DemoDbContext ctx)
      {
         var customerOrder = await ctx.Customers
                                      .LeftJoin(ctx.Orders,
                                                c => c.Id,
                                                o => o.CustomerId,
                                                result => new { Customer = result.Left, Order = result.Right })
                                      .ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customerOrder.Select(co => $"{{ CustomerId={co.Customer.Id}, OrderId={co.Order?.Id} }}"))}");
      }

      private static async Task DoBulkInsertIntoRealTableAsync([NotNull] DemoDbContext ctx)
      {
         var customersToInsert = new Customer { Id = Guid.NewGuid() };
         await ctx.BulkInsertAsync(new[] { customersToInsert });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customers: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertSpecifiedColumnsIntoRealTableAsync([NotNull] DemoDbContext ctx)
      {
         var customersToInsert = new Customer { Id = Guid.NewGuid() };

         // only "Id" is sent to the DB
         // alternative ways to specify the column:
         // * c => new { c.Id }
         // * c => c.Id
         // * new SqlBulkInsertOptions { PropertiesProvider = PropertiesProvider.From<Customer>(c => new { c.Id })}
         await ctx.BulkInsertAsync(new[] { customersToInsert }, c => new { c.Id });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customers: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertIntoTempTableAsync([NotNull] DemoDbContext ctx, [NotNull] List<Guid> customerIds)
      {
         var tempTableQuery = await ctx.BulkInsertValuesIntoTempTableAsync(customerIds);
         var customers = await ctx.Customers.Join(tempTableQuery, c => c.Id, t => t.Column1, (c, t) => c).ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customers.Select(c => c.Id))}");
      }

      private static async Task DoBulkInsertIntoTempTableAsync([NotNull] DemoDbContext ctx, [NotNull] List<(Guid customerId, Guid productId)> tuples)
      {
         var tempTableQuery = await ctx.BulkInsertValuesIntoTempTableAsync(tuples);
         var orderItems = await ctx.OrderItems.Join(tempTableQuery,
                                                    i => new { i.Order.CustomerId, i.ProductId },
                                                    t => new { CustomerId = t.Column1, ProductId = t.Column2 },
                                                    (i, t) => i)
                                   .ToListAsync();

         Console.WriteLine($"Found order items: {String.Join(", ", orderItems.Select(i => $"{{ OrderId={i.OrderId}, ProductId={i.ProductId}, Count={i.Count} }}"))}");
      }

      private static async Task DoBulkInsertEntitiesIntoTempTableAsync([NotNull] DemoDbContext ctx)
      {
         var customersToInsert = new[] { new Customer { Id = Guid.NewGuid() } };

         var tempTableQuery = await ctx.BulkInsertIntoTempTableAsync(customersToInsert);
         var tempCustomers = await tempTableQuery.ToListAsync();

         Console.WriteLine($"Customers in temp table: {String.Join(", ", tempCustomers.Select(c => c.Id))}");
      }
   }
}
