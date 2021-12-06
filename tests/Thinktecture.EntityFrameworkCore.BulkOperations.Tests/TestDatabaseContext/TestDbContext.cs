using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext;

public class TestDbContext : DbContext
{
   public DbSet<TestEntity> TestEntities { get; set; } = null!;

   public TestDbContext(DbContextOptions<TestDbContext> options)
      : base(options)
   {
   }

   /// <inheritdoc />
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<TestEntity>().Property(e => e.ConvertibleClass)
                  .HasConversion(c => c!.Key, k => new ConvertibleClass(k));
   }
}