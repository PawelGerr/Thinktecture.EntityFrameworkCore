using Microsoft.EntityFrameworkCore.Design;

namespace Thinktecture.Database;

public class DemoDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
{
   public DemoDbContext CreateDbContext(string[] args)
   {
      var options = new DbContextOptionsBuilder<DemoDbContext>()
                    .UseSqlite(SamplesContext.Instance.ConnectionString)
                    .AddSchemaRespectingComponents()
                    .Options;

      return new DemoDbContext(options);
   }
}