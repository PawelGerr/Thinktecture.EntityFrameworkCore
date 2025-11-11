using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;

namespace Thinktecture;

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
         // have to keep open the connection because we are using an in-memory SQLite database
         await ctx.Database.OpenConnectionAsync();

         try
         {
            await ctx.Database.MigrateAsync();

            var customerId = await ctx.EnsureCustomerAsync(new Guid("11D67C68-6F1A-407B-9BD3-56C84FE15BB1"));
            var productId = await ctx.EnsureProductAsync(new Guid("872BCAC2-1A85-4B22-AC0F-7D920563A000"));
            var orderId = await ctx.EnsureOrderAsync(new Guid("EC1CBF87-F53F-4EF4-B286-8F5EB0AE810D"), customerId);
            await ctx.EnsureOrderItemAsync(orderId, productId, 42);
            ctx.ChangeTracker.Clear(); // resetting DbContext, as an alternative to create a new one

            // Bulk insert into temp tables
            await BulkInsertIntoTempTableAsync(ctx);
            ctx.ChangeTracker.Clear();

            // Bulk insert into "real" tables
            await DoBulkInsertAsync(ctx);
            ctx.ChangeTracker.Clear();

            await DoBulkInsertSpecificColumnsAsync(ctx);
            ctx.ChangeTracker.Clear();

            // Bulk update
            await DoBulkUpdateAsync(ctx, customerId);
            ctx.ChangeTracker.Clear();

            // Bulk insert or update
            await DoBulkInsertOrUpdateAsync(ctx, customerId);
            ctx.ChangeTracker.Clear();

            // ROWNUMBER
            await DoRowNumberAsync(ctx);
            ctx.ChangeTracker.Clear();
         }
         finally
         {
            await ctx.Database.CloseConnectionAsync();
         }
      }

      Console.WriteLine("Exiting samples...");
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

   private static async Task BulkInsertIntoTempTableAsync(DemoDbContext ctx)
   {
      var customersToInsert = new Customer(Guid.NewGuid(), "First name", "Last name");
      await using var tempTable = await ctx.BulkInsertIntoTempTableAsync(new[] { customersToInsert });

      var insertedCustomer = await tempTable.Query.FirstAsync(c => c.Id == customersToInsert.Id);

      Console.WriteLine($"Customer from temp table: {insertedCustomer.Id}");
   }

   private static async Task DoBulkInsertAsync(DemoDbContext ctx)
   {
      var customersToInsert = new Customer(Guid.NewGuid(), "First name", "Last name");
      await ctx.BulkInsertAsync(new[] { customersToInsert });

      var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

      Console.WriteLine($"Inserted customer: {insertedCustomer.Id}");
   }

   private static async Task DoBulkInsertSpecificColumnsAsync(DemoDbContext ctx)
   {
      var customersToInsert = new Customer(Guid.NewGuid(), "First name", "Last name");

      // only "Id" is sent to the DB
      // alternative ways to specify the column:
      // * c => new { c.Id }
      // * c => c.Id
      // * new SqliteBulkInsertOptions { PropertiesToInsert = IPropertiesProvider.Include<Customer>(c => new { c.Id })}
      await ctx.BulkInsertAsync(new[] { customersToInsert }, c => new { c.Id, c.FirstName, c.LastName });

      var insertedCustomer = await ctx.Customers.FirstAsync(c => c.Id == customersToInsert.Id);

      Console.WriteLine($"Inserted customer: {insertedCustomer.Id}");
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
}
