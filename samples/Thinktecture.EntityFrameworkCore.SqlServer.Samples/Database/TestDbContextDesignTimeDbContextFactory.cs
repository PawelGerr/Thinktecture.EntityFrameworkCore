using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thinktecture.Database
{
   public class TestDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
   {
      [NotNull]
      public TestDbContext CreateDbContext(string[] args)
      {
         var options = new DbContextOptionsBuilder<TestDbContext>()
                       .UseSqlServer(SamplesContext.Instance.ConnectionString)
                       .Options;

         return new TestDbContext(options);
      }
   }
}
