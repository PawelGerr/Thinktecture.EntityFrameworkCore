namespace Thinktecture.Database;

public class SqliteBenchmarkDbContext : BenchmarkDbContext
{
   public SqliteBenchmarkDbContext(DbContextOptions<SqliteBenchmarkDbContext> options)
      : base(options)
   {
   }
}
