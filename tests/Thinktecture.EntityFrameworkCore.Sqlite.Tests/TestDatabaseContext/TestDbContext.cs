using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbContext : DbContext
   {
#nullable disable
      public DbSet<TestEntity> TestEntities { get; set; }
      public DbSet<TestEntityWithAutoIncrement> TestEntitiesWithAutoIncrement { get; set; }
      public DbSet<TestEntityWithShadowProperties> TestEntitiesWithShadowProperties { get; set; }
#nullable enable

      public Action<ModelBuilder>? ConfigureModel { get; set; }

      public TestDbContext(DbContextOptions<TestDbContext> options)
         : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         modelBuilder.Entity<TestEntity>().Property("_privateField");

         modelBuilder.Entity<TestEntityWithAutoIncrement>().Property(e => e.Id).ValueGeneratedOnAdd();

         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<string>("ShadowStringProperty").HasMaxLength(50);
         modelBuilder.Entity<TestEntityWithShadowProperties>().Property<int?>("ShadowIntProperty");

         ConfigureModel?.Invoke(modelBuilder);
      }
   }
}
