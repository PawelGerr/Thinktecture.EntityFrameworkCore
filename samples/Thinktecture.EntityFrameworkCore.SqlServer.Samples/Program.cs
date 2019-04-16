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
         var sp = CreateServiceProvider();

         using (var scope = sp.CreateScope())
         {
            var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            await DoBulkInsertAsync(ctx);
         }

         Console.WriteLine("Exiting samples...");
      }

      private static async Task DoBulkInsertAsync(TestDbContext ctx)
      {
         var query = await ctx.BulkInsertTempTableAsync(new List<Guid>() { new Guid("11D67C68-6F1A-407B-9BD3-56C84FE15BB1") });

         var records = await query.ToListAsync();
         Console.WriteLine($"Records: {String.Join(", ", records.Select(r => r.Column1))}");
      }

      private static IServiceProvider CreateServiceProvider()
      {
         var services = new ServiceCollection()
            .AddDbContext<TestDbContext>(builder => builder.UseSqlServer("server=localhost;database=test;integrated security=true"));

         return services.BuildServiceProvider();
      }
   }
}
