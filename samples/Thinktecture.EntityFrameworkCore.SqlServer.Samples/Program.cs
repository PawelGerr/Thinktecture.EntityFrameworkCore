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
         var sp = SamplesContext.Instance.CreateServiceProvider("demo");

         using (var scope = sp.CreateScope())
         {
            var ctx = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            await ctx.Database.MigrateAsync();

            await FetchRowVersionsAsync(ctx);

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

            // COUNT DISTINCT
            await DoCountDistinctAsync(ctx);

            // Tenant
            await DoTenantQueriesAsync(ctx);

            // Nested transactions
            await DoNestedTransactionsAsync(ctx, orderId);
         }

         Console.WriteLine("Exiting samples...");
      }

      private static async Task DoTenantQueriesAsync(DemoDbContext ctx)
      {
         var customers = await ctx.Customers
                                  .Include(c => c.Orders)
                                  .ToListAsync();

         try
         {
            CurrentTenant.Value = "1";

            customers = await ctx.Customers
                                 .Include(c => c.Orders)
                                 .ToListAsync();

            CurrentTenant.Value = "2";

            customers = await ctx.Customers
                                 .Include(c => c.Orders)
                                 .ToListAsync();
         }
         finally
         {
            CurrentTenant.Value = null;
         }
      }

      private static async Task DoNestedTransactionsAsync(DemoDbContext ctx, Guid orderId)
      {
         await using var tx = await ctx.Database.BeginTransactionAsync();

         await using var innerTx = await ctx.Database.BeginTransactionAsync();

         var order = await ctx.Orders.FirstAsync(c => c.Id == orderId);
         order.Text = $"Changed ({DateTime.Now})";

         await ctx.SaveChangesAsync();

         innerTx.Commit();

         tx.Commit();
      }

      private static async Task FetchRowVersionsAsync(DemoDbContext ctx)
      {
         var minActiveRowVersion = await ctx.GetMinActiveRowVersionAsync();
         Console.WriteLine($"Min active row version: {minActiveRowVersion}");

         var lastUsedRowVersion = await ctx.GetLastUsedRowVersionAsync();
         Console.WriteLine($"Last used row version: {lastUsedRowVersion}");
      }

      private static async Task DoCountDistinctAsync(DemoDbContext ctx)
      {
         var numberOfCustomerIds = await ctx.Orders.GroupBy(o => o.Date)
                                            .Select(g => g.CountDistinct(o => o.CustomerId))
                                            .ToListAsync();

         Console.WriteLine($"COUNT DISTINCT: [{String.Join(", ", numberOfCustomerIds)}]");
      }

      private static async Task DoRowNumberAsync(DemoDbContext ctx)
      {
         var customers = await ctx.Customers
                                  .Select(c => new
                                               {
                                                  c.Id,
                                                  FirstName_RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(c.FirstName)),
                                                  LastName_RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(c.LastName)),
                                                  FirstAndLastName1_RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(c.FirstName + " " + c.LastName)),
                                                  FirstAndLastName2_RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(c.FirstName).ThenBy(c.LastName))
                                               })
                                  .AsSubQuery()
                                  .OrderBy(c => c.FirstName_RowNumber)
                                  .ThenBy(c => c.LastName_RowNumber)
                                  .ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customers.Select(c => $"{{ CustomerId={c.Id}, FirstName_RowNumber={c.FirstName_RowNumber}, LastName_RowNumber={c.LastName_RowNumber}, FirstAndLastName1_RowNumber={c.FirstAndLastName1_RowNumber}, FirstAndLastName2_RowNumber={c.FirstAndLastName2_RowNumber} }}"))}");

         var latestOrders = await ctx.Orders
                                     .Select(o => new
                                                  {
                                                     o.Id,
                                                     o.CustomerId,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(o.Date))
                                                  })
                                     // Previous query must be a sub query to access "RowNumber"
                                     .AsSubQuery()
                                     .Where(i => i.RowNumber == 1)
                                     .ToListAsync();
         Console.WriteLine($"Latest orders: {String.Join(", ", latestOrders.Select(o => $"{{ CustomerId={o.CustomerId}, OrderId={o.Id} }}"))}");
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

      private static async Task DoBulkInsertIntoRealTableAsync(DemoDbContext ctx)
      {
         var id = Guid.NewGuid();
         var customersToInsert = new Customer(id, $"First name of '{id}'", $"Last name of '{id}'");
         await ctx.BulkInsertAsync(new[] { customersToInsert });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customers: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertSpecifiedColumnsIntoRealTableAsync(DemoDbContext ctx)
      {
         var customersToInsert = new Customer(Guid.NewGuid(), "First name", "Last name");

         // only "Id" is sent to the DB
         // alternative ways to specify the column:
         // * c => new { c.Id }
         // * c => c.Id
         // * new SqlBulkInsertOptions { PropertiesProvider = PropertiesProvider.From<Customer>(c => new { c.Id })}
         await ctx.BulkInsertAsync(new[] { customersToInsert }, c => new { c.Id });

         var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

         Console.WriteLine($"Inserted customers: {insertedCustomer.Id}");
      }

      private static async Task DoBulkInsertIntoTempTableAsync(DemoDbContext ctx, List<Guid> customerIds)
      {
         await using var tempTableQuery = await ctx.BulkInsertValuesIntoTempTableAsync(customerIds);

         var customers = await ctx.Customers.Join(tempTableQuery.Query, c => c.Id, t => t.Column1, (c, t) => c).ToListAsync();
         Console.WriteLine($"Found customers: {String.Join(", ", customers.Select(c => c.Id))}");
      }

      private static async Task DoBulkInsertIntoTempTableAsync(DemoDbContext ctx, List<(Guid customerId, Guid productId)> tuples)
      {
         await using var tempTableQuery = await ctx.BulkInsertValuesIntoTempTableAsync(tuples);

         var orderItems = await ctx.OrderItems.Join(tempTableQuery.Query,
                                                    i => new { i.Order.CustomerId, i.ProductId },
                                                    t => new { CustomerId = t.Column1, ProductId = t.Column2 },
                                                    (i, t) => i)
                                   .ToListAsync();

         Console.WriteLine($"Found order items: {String.Join(", ", orderItems.Select(i => $"{{ OrderId={i.OrderId}, ProductId={i.ProductId}, Count={i.Count} }}"))}");
      }

      private static async Task DoBulkInsertEntitiesIntoTempTableAsync(DemoDbContext ctx)
      {
         var id = Guid.NewGuid();
         var customersToInsert = new[]
                                 {
                                    new Customer(id, $"First name of '{id}'", $"Last name of '{id}'")
                                 };

         await using var tempTableQuery = await ctx.BulkInsertIntoTempTableAsync(customersToInsert);

         var tempCustomers = await tempTableQuery.Query.ToListAsync();

         Console.WriteLine($"Customers in temp table: {String.Join(", ", tempCustomers.Select(c => c.Id))}");
      }
   }
}
