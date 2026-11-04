using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;

public sealed class TempTableQueryTests
{
   [Fact]
   public void Should_translate_from_temp_table_name()
   {
      using var context = CreateContext();
      var sql = context.Entities.FromTempTable(new TempTableInfo("my_temp_table")).ToQueryString();

      sql.Should().Contain("`my_temp_table`");
      sql.Should().NotContain("`Entities` AS");
   }

   [Fact]
   public void Should_translate_subquery_after_temp_table_reference()
   {
      using var context = CreateContext();
      var sql = context.Entities.FromTempTable(new TempTableInfo("my_temp_table"))
                        .AsSubQuery()
                        .Where(entity => entity.Id > 1)
                        .ToQueryString();

      sql.Should().Contain("`my_temp_table`");
      sql.Should().Contain("WHERE");
   }

   private static MySqlTestDbContext CreateContext()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport())
                    .Options;

      return new MySqlTestDbContext(options);
   }
}
