using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thinktecture.TestDatabaseContext
{
   // ReSharper disable once UnusedMember.Global
   public class TestDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
   {
      [NotNull]
      public TestDbContext CreateDbContext(string[] args)
      {
         var options = new DbContextOptionsBuilder<TestDbContext>()
                       .UseSqlite("DataSource=:memory:")
                       .Options;

         return new TestDbContext(options);
      }
   }
}
