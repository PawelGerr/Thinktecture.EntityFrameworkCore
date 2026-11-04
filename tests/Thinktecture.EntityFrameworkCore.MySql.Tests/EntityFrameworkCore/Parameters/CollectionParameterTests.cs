using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Parameters;

public sealed class CollectionParameterTests
{
   [Fact]
   public void Should_generate_scalar_json_table_query_with_limit()
   {
      using var context = CreateContext();
      var query = context.GetService<ICollectionParameterFactory>()
                         .CreateScalarQuery(context, new[] { 1, 2, 3 }, applyDistinct: false);
      var sql = query.ToQueryString();

      sql.Should().Contain("JSON_TABLE");
      sql.Should().Contain("LIMIT 3");
      sql.Should().NotContain("SELECT DISTINCT");
   }

   [Fact]
   public void Should_apply_distinct_to_scalar_collection_query()
   {
      using var context = CreateContext();
      var query = context.GetService<ICollectionParameterFactory>()
                         .CreateScalarQuery(context, new[] { 1, 1, 2 }, applyDistinct: true);
      var sql = query.ToQueryString();

      sql.Should().Contain("SELECT DISTINCT");
      sql.Should().Contain("LIMIT 3");
   }

   [Fact]
   public void Should_generate_empty_scalar_collection_query()
   {
      using var context = CreateContext();
      var query = context.GetService<ICollectionParameterFactory>()
                         .CreateScalarQuery(context, Array.Empty<int>(), applyDistinct: false);
      var sql = query.ToQueryString();

      sql.Should().Contain("JSON_TABLE");
      sql.Should().Contain("LIMIT 0");
   }

   [Fact]
   public void Should_generate_complex_json_table_query()
   {
      using var context = CreateContext();
      var query = context.GetService<ICollectionParameterFactory>()
                         .CreateComplexQuery(context, new[] { new MySqlTestComplexParameter(1) }, applyDistinct: false);
      var sql = query.ToQueryString();

      sql.Should().Contain("JSON_TABLE");
      sql.Should().Contain("PATH '$.\"Id\"'");
      sql.Should().NotContain("SELECT DISTINCT");
   }

   [Fact]
   public void Should_apply_distinct_to_complex_collection_query()
   {
      using var context = CreateContext();
      var query = context.GetService<ICollectionParameterFactory>()
                         .CreateComplexQuery(context, new[] { new MySqlTestComplexParameter(1) }, applyDistinct: true);
      var sql = query.ToQueryString();

      sql.Should().Contain("SELECT DISTINCT");
      sql.Should().Contain("JSON_TABLE");
   }

   private static MySqlTestDbContext CreateContext()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddCollectionParameterSupport())
                    .Options;

      return new MySqlTestDbContext(options);
   }
}