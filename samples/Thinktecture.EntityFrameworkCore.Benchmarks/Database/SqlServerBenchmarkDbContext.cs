namespace Thinktecture.Database;

public class SqlServerBenchmarkDbContext : BenchmarkDbContext
{
   public SqlServerBenchmarkDbContext(DbContextOptions<SqlServerBenchmarkDbContext> options)
      : base(options)
   {
   }

   /// <inheritdoc />
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.ConfigureScalarCollectionParameter<decimal>(builder => builder.Property(e => e.Value).HasPrecision(10, 4));

      modelBuilder.ConfigureComplexCollectionParameter<MyParameter>();

      modelBuilder.ConfigureTempTableEntity<MyParameter>();
   }
}
