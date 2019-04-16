using System;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.Database
{
   public class TestDbContext : DbContext
   {
      public TestDbContext(DbContextOptions<TestDbContext> options)
         : base(options)
      {
      }

      /// <inheritdoc />
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.ConfigureTempTable<Guid>();
      }
   }
}
