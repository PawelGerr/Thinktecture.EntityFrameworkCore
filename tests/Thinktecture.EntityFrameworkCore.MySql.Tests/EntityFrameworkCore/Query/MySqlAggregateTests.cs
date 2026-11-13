namespace Thinktecture.EntityFrameworkCore.Query;

public sealed class MySqlAggregateTests
{
   [Fact]
   public void Should_translate_bit_or()
   {
      var sql = CreateContext().Entities
                               .GroupBy(entity => entity.Name)
                               .Select(group => EF.Functions.BitOr(group.Select(entity => entity.Id)))
                               .ToQueryString();

      sql.Should().Contain("BIT_OR(");
   }

   [Fact]
   public void Should_translate_bit_and()
   {
      var sql = CreateContext().Entities
                               .GroupBy(entity => entity.Name)
                               .Select(group => EF.Functions.BitAnd(group.Select(entity => entity.Id)))
                               .ToQueryString();

      sql.Should().Contain("BIT_AND(");
   }

   [Fact]
   public void Should_translate_bit_xor()
   {
      var sql = CreateContext().Entities
                               .GroupBy(entity => entity.Name)
                               .Select(group => EF.Functions.BitXor(group.Select(entity => entity.Id)))
                               .ToQueryString();

      sql.Should().Contain("BIT_XOR(");
   }

   private static MySqlTestDbContext CreateContext()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddWindowFunctionsSupport())
                    .Options;

      return new MySqlTestDbContext(options);
   }
}