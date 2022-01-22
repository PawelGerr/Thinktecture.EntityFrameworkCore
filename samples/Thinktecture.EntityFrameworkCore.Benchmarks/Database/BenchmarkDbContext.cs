namespace Thinktecture.Database;

public abstract class BenchmarkDbContext : DbContext
{
   public DbSet<TestEntity> TestEntities { get; set; } = null!;
   public DbSet<EntityWithByteArray> EntitiesWithByteArray { get; set; } = null!;
   public DbSet<EntityWithByteArrayAndValueComparer> EntitiesWithByteArrayAndValueComparer { get; set; } = null!;

   protected BenchmarkDbContext(DbContextOptions options)
      : base(options)
   {
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.ConfigureTempTable<int, int>();

      modelBuilder.Entity<EntityWithByteArray>(builder =>
                                               {
                                                  builder.ToTable("EntitiesWithByteArray");
                                               });
      modelBuilder.Entity<EntityWithByteArrayAndValueComparer>(builder =>
                                                               {
                                                                  builder.ToTable("EntitiesWithByteArrayAndValueComparer");

                                                                  builder.Property(e => e.Bytes)
                                                                         .UseReferenceEqualityComparer();
                                                               });
   }
}
