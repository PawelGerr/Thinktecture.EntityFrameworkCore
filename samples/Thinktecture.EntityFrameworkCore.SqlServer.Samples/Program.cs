using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;

namespace Thinktecture;

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
         ctx.ChangeTracker.Clear(); // resetting DbContext, as an alternative to create a new one

         // Bulk insert into "real" tables
         await DoBulkInsertIntoRealTableAsync(ctx);
         ctx.ChangeTracker.Clear();

         await DoBulkInsertSpecifiedColumnsIntoRealTableAsync(ctx);
         ctx.ChangeTracker.Clear();

         // Bulk update
         await DoBulkUpdateAsync(ctx, customerId);
         ctx.ChangeTracker.Clear();

         // Bulk insert or update
         await DoBulkInsertOrUpdateAsync(ctx, customerId);
         ctx.ChangeTracker.Clear();

         // Bulk delete
         await DoBulkDeleteAsync(ctx);
         ctx.ChangeTracker.Clear();

         // Bulk insert into temp tables
         await DoBulkInsertEntitiesIntoTempTableAsync(ctx);
         ctx.ChangeTracker.Clear();

         await DoBulkInsertIntoTempTableAsync(ctx, new List<Guid> { customerId });
         ctx.ChangeTracker.Clear();

         await DoBulkInsertIntoTempTableAsync(ctx, new List<(Guid, Guid)> { (customerId, productId) });
         ctx.ChangeTracker.Clear();

         // LEFT JOIN
         await DoLeftJoinAsync(ctx);
         ctx.ChangeTracker.Clear();

         // ROWNUMBER
         await DoRowNumberAsync(ctx);
         ctx.ChangeTracker.Clear();

         // Tenant
         await DoTenantQueriesAsync(ctx);
         ctx.ChangeTracker.Clear();

         // Nested transactions
         await DoNestedTransactionsAsync(ctx, orderId);
         ctx.ChangeTracker.Clear();
      }

      Console.WriteLine("Exiting samples...");
   }

   private static async Task DoTenantQueriesAsync(DemoDbContext ctx)
   {
      await ctx.Customers
               .Include(c => c.Orders)
               .ToListAsync();

      try
      {
         // requires a database with the name "demo"
         CurrentTenant.Value = "1";

         await ctx.Customers
                  .Include(c => c.Orders)
                  .ToListAsync();

         // requires a database with the name "demo2"
         CurrentTenant.Value = "2";

         await ctx.Customers
                  .Include(c => c.Orders)
                  .ToListAsync();
      }
      catch
      {
         Console.WriteLine("For this demo we need 2 databases: demo and demo2");
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

      await innerTx.CommitAsync();

      await tx.CommitAsync();
   }

   private static async Task FetchRowVersionsAsync(DemoDbContext ctx)
   {
      var minActiveRowVersion = await ctx.GetMinActiveRowVersionAsync();
      Console.WriteLine($"Min active row version: {minActiveRowVersion}");

      var lastUsedRowVersion = await ctx.GetLastUsedRowVersionAsync();
      Console.WriteLine($"Last used row version: {lastUsedRowVersion}");
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
      // * new SqlServerBulkInsertOptions { PropertiesToInsert = IPropertiesProvider.Include<Customer>(c => new { c.Id })}
      await ctx.BulkInsertAsync(new[] { customersToInsert }, c => new { c.Id });

      var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

      Console.WriteLine($"Inserted customers: {insertedCustomer.Id}");
   }

   private static async Task DoBulkDeleteAsync(DemoDbContext ctx)
   {
      ctx.Add(new Customer(Guid.NewGuid(), "Customer To Delete", "Test"));
      await ctx.SaveChangesAsync();

      var affectedRows = await ctx.Customers
                                  .Where(c => c.FirstName == "Customer To Delete")
                                  .BulkDeleteAsync();

      Console.WriteLine($"Number of deleted customers: {affectedRows}");
   }

   private static async Task DoBulkInsertOrUpdateAsync(DemoDbContext ctx, Guid customerId)
   {
      var customer = new Customer(customerId, "First name - DoBulkInsertOrUpdateAsync", "Last name will not be updated");
      var newCustomer = new Customer(Guid.NewGuid(), "First name - DoBulkInsertOrUpdateAsync", "Last name - DoBulkInsertOrUpdateAsync");

      await ctx.BulkInsertOrUpdateAsync(new[] { newCustomer, customer }, propertiesToUpdate: c => c.FirstName);

      var customers = await ctx.Customers.Where(c => c.Id == customerId || c.Id == newCustomer.Id).ToListAsync();

      Console.WriteLine($"Updated customer: {customers.Single(c => c.Id == customerId)}");
      Console.WriteLine($"New customer: {customers.Single(c => c.Id == newCustomer.Id)}");
   }

   private static async Task DoBulkUpdateAsync(DemoDbContext ctx, Guid customerId)
   {
      var customer = new Customer(customerId, "First name - DoBulkUpdateAsync", "Last name will not be updated");

      await ctx.BulkUpdateAsync(new[] { customer }, c => c.FirstName);

      var updatedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customerId);

      Console.WriteLine($"Updated customer: {updatedCustomer}");
   }

   private static async Task DoBulkInsertIntoTempTableAsync(DemoDbContext ctx, List<Guid> customerIds)
   {
      await using var tempTableQuery = await ctx.BulkInsertValuesIntoTempTableAsync(customerIds);

      var customers = await ctx.Customers.Join(tempTableQuery.Query, c => c.Id, t => t, (c, t) => c).ToListAsync();
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
