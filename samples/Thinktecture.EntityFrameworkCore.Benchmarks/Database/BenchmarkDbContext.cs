namespace Thinktecture.Database;

public abstract class BenchmarkDbContext : DbContext
{
   public DbSet<TestEntity> TestEntities { get; set; } = null!;

   protected BenchmarkDbContext(DbContextOptions options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.ConfigureTempTable<int>();
      modelBuilder.ConfigureTempTable<int, int>();
   }
}
