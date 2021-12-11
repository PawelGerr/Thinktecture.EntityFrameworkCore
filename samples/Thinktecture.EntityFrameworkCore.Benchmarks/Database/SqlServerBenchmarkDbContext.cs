namespace Thinktecture.Database;

public class SqlServerBenchmarkDbContext : BenchmarkDbContext
{
   public SqlServerBenchmarkDbContext(DbContextOptions<SqlServerBenchmarkDbContext> options)
      : base(options)
   {
   }
}
