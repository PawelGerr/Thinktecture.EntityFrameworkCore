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

      modelBuilder.ConfigureComplexCollectionParameter<MyParameter>();

      modelBuilder.ConfigureTempTableEntity<MyParameter>();
   }
}
