using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
   {
      [NotNull]
      public TestDbContext CreateDbContext(string[] args)
      {
         var options = new DbContextOptionsBuilder<TestDbContext>()
                       .UseSqlServer(TestContext.Instance.ConnectionString)
                       .AddSchemaAwareSqlServerComponents()
                       .Options;

         return new TestDbContext(options, null);
      }
   }
}
