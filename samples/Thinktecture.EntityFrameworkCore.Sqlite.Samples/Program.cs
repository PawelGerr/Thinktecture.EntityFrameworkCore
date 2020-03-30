using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
         var sp = SamplesContext.Instance.CreateServiceProvider();

         using (var scope = sp.CreateScope())
         {
            var ctx = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            await ctx.Database.OpenConnectionAsync();

            try
            {
               await ctx.Database.MigrateAsync();

               var customerId = await ctx.EnsureCustomerAsync(new Guid("11D67C68-6F1A-407B-9BD3-56C84FE15BB1"));
               var productId = await ctx.EnsureProductAsync(new Guid("872BCAC2-1A85-4B22-AC0F-7D920563A000"));
               var orderId = await ctx.EnsureOrderAsync(new Guid("EC1CBF87-F53F-4EF4-B286-8F5EB0AE810D"), customerId);
               await ctx.EnsureOrderItemAsync(orderId, productId, 42);

               // Bulk insert into temp tables
               await DoBulkInsertIntoTempTableAsync(ctx);

               // Bulk insert into "real" tables
               await DoBulkInsertIntoRealTableAsync(ctx);
               await DoBulkInsertSpecifiedColumnsIntoRealTableAsync(ctx);

               // LEFT JOIN
               await DoLeftJoinAsync(ctx);
            }
            finally
            {
               ctx.Database.CloseConnection();
            }
         }

         Console.WriteLine("Exiting samples...");
      }

      private static async Task DoLeftJoinAsync(DemoDbContext ctx)
      {
         var customerOrder = await ctx.Customers
                                      .LeftJoin(ctx.Orders,
                                                c => c.Id,
                                                o => o.CustomerId,
                                                result => new { Customer = result.Left, Order = result.Right })
                                      .ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customerOrder.Select(co => $"{{ CustomerId={co.Customer.Id}, OrderId={co.Order?.Id} }}"))}");
      }

      private static async Task DoBulkInsertIntoTempTableAsync(DemoDbContext ctx)
      {
         var customersToInsert = new Customer { Id = Guid.NewGuid() };
         await using var tempTable = await ctx.BulkInsertIntoTempTableAsync(new[] { customersToInsert });

         var insertedCustomer = await tempTable.Query.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Customer from temp table: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertIntoRealTableAsync(DemoDbContext ctx)
      {
         var customersToInsert = new Customer { Id = Guid.NewGuid() };
         await ctx.BulkInsertAsync(new[] { customersToInsert });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customer: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertSpecifiedColumnsIntoRealTableAsync(DemoDbContext ctx)
      {
         var customersToInsert = new Customer { Id = Guid.NewGuid() };

         // only "Id" is sent to the DB
         // alternative ways to specify the column:
         // * c => new { c.Id }
         // * c => c.Id
         // * new SqlBulkInsertOptions { PropertiesProvider = PropertiesProvider.From<Customer>(c => new { c.Id })}
         await ctx.BulkInsertAsync(new[] { customersToInsert }, c => new { c.Id });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customer: {insertedCustomer.Id}");
      }
   }
}
