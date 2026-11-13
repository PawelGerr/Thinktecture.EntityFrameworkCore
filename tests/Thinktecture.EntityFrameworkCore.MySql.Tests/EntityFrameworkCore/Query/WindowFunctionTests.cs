namespace Thinktecture.EntityFrameworkCore.Query;

public sealed class WindowFunctionTests
{
   private static readonly WindowFunction<long> _rank = new("RANK");
   private static readonly WindowFunction<int> _lead = new("LEAD");
   private static readonly WindowFunction<long> _countAll = new("COUNT", useAsteriskWhenNoArguments: true);

   [Fact]
   public void Should_generate_row_number_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => new { entity.Id, RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(entity.Id)) })
                       .ToQueryString();

      sql.Should().Contain("ROW_NUMBER() OVER(ORDER BY");
   }

   [Fact]
   public void Should_generate_custom_window_function_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => new { entity.Id, Rank = EF.Functions.WindowFunction(_rank, EF.Functions.OrderBy(entity.Id)) })
                       .ToQueryString();

      sql.Should().Contain("RANK() OVER");
   }

   [Fact]
   public void Should_generate_multi_argument_window_function_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => EF.Functions.WindowFunction(_lead, entity.Id, 1, EF.Functions.OrderBy(entity.Id)))
                       .ToQueryString();

      sql.Should().Contain("LEAD(");
      sql.Should().Contain("OVER");
   }

   [Fact]
   public void Should_generate_partition_only_window_function_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => EF.Functions.WindowFunction(_countAll, EF.Functions.PartitionBy(entity.Name)))
                       .ToQueryString();

      sql.Should().Contain("COUNT(*) OVER");
      sql.Should().Contain("PARTITION BY");
   }

   [Fact]
   public void Should_generate_ntile_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => new { entity.Id, Bucket = EF.Functions.NTile(2, EF.Functions.OrderBy(entity.Id)) })
                       .ToQueryString();

      sql.Should().Contain("NTILE(");
      sql.Should().Contain("OVER");
   }

   [Fact]
   public void Should_generate_partitioned_ntile_sql()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      using var context = new MySqlTestDbContext(options);
      var sql = context.Entities
                       .Select(entity => new { entity.Id, Bucket = EF.Functions.NTile(2, entity.Name, EF.Functions.OrderBy(entity.Id)) })
                       .ToQueryString();

      sql.Should().Contain("NTILE(");
      sql.Should().Contain("PARTITION BY");
      sql.Should().Contain("ORDER BY");
   }

}