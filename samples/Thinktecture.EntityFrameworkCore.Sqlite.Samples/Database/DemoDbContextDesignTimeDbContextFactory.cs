using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thinktecture.Database
{
   public class DemoDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
   {
      [NotNull]
      public DemoDbContext CreateDbContext(string[] args)
      {
         var options = new DbContextOptionsBuilder<DemoDbContext>()
                       .UseSqlite(SamplesContext.Instance.ConnectionString)
                       .AddSchemaRespectingComponents()
                       .Options;

         return new DemoDbContext(options);
      }
   }
}
