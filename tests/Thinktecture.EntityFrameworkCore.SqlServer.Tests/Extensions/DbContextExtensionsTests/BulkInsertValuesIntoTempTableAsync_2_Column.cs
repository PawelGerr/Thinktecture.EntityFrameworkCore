using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertValuesIntoTempTableAsync_2_Column : IntegrationTestsBase
{
   public BulkInsertValuesIntoTempTableAsync_2_Column(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public async Task Should_insert_int_nullable_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

      var values = new List<(int, int?)> { (1, null) };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlServerTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { new TempTable<int, int?>(1, null) });
   }

   [Fact]
   public async Task Should_insert_string_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string, string>(typeBuilder => typeBuilder.Property(t => t.Column2).IsRequired(false));

      var values = new List<(string, string?)> { ("value1", null) };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlServerTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { new TempTable<string, string?>("value1", null) });
   }

   [Fact]
   public async Task Should_create_pk_by_default()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int, int>(false);

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<(int, int)> { (1, 2) }, new SqlServerTempTableBulkInsertOptions { TableNameProvider = DefaultTempTableNameProvider.Instance });

      var keys = AssertDbContext.GetTempTableKeyColumns<int, int>().ToList();
      keys.Should().HaveCount(2);
      keys[0].COLUMN_NAME.Should().Be(nameof(TempTable<int, int>.Column1));
      keys[1].COLUMN_NAME.Should().Be(nameof(TempTable<int, int>.Column2));
   }
}
