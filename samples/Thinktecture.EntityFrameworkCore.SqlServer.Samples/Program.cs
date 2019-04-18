using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.Database;

namespace Thinktecture
{
   class Program
   {
      static async Task Main(string[] args)
      {
         var sp = SamplesContext.Instance.CreateServiceProvider();

         using (var scope = sp.CreateScope())
         {
            var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await ctx.Database.MigrateAsync();

            var customerIds = new List<Guid> { await ctx.EnsureCustomerAsync(new Guid("11D67C68-6F1A-407B-9BD3-56C84FE15BB1")) };
            await DoBulkInsertAsync(ctx, customerIds);
         }

         Console.WriteLine("Exiting samples...");
      }

      private static async Task DoBulkInsertAsync(TestDbContext ctx, List<Guid> customerIds)
      {
         var tempTableQuery = await ctx.BulkInsertTempTableAsync(customerIds);
         var customers = await ctx.Customers.Join(tempTableQuery, c => c.Id, t => t.Column1, (c, t) => c).ToListAsync();

         Console.WriteLine($"Found customers: {String.Join(", ", customers.Select(r => r.Id))}");
      }
   }
}
