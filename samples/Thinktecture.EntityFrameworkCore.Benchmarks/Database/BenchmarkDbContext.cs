namespace Thinktecture.Database;

public abstract class BenchmarkDbContext : DbContext
{
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
