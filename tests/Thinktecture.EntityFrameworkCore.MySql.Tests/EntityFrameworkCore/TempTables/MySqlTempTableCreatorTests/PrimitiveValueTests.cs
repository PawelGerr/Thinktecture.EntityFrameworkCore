using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class PrimitiveValueTests : MySqlIntegrationTestsBase
{
   public PrimitiveValueTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_guids_into_a_temp_table()
   {
      var first = Guid.NewGuid();
      var second = Guid.NewGuid();
      var executor = ActDbContext.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertValuesIntoTempTableAsync(
                                                                                    [first, second],
                                                                                    executor.CreateOptions(),
                                                                                    CancellationToken.None);

      (await tempTable.Query.ToArrayAsync()).Should().BeEquivalentTo([first, second]);
   }

   [Fact]
   public async Task Should_insert_precise_decimals_into_a_temp_table()
   {
      var executor = ActDbContext.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertValuesIntoTempTableAsync(
                                                                                    [12345678901234567.89m, 1.25m],
                                                                                    executor.CreateOptions(),
                                                                                    CancellationToken.None);

      (await tempTable.Query.OrderBy(value => value).ToArrayAsync()).Should().Equal(1.25m, 12345678901234567.89m);
   }

   [Fact]
   public async Task Should_insert_date_times_into_a_temp_table()
   {
      var value = new DateTime(2020, 1, 2, 3, 4, 5).AddTicks(1234560);
      var executor = ActDbContext.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertValuesIntoTempTableAsync(
                                                                                    [value],
                                                                                    executor.CreateOptions(),
                                                                                    CancellationToken.None);

      (await tempTable.Query.SingleAsync()).Should().Be(value);
   }
}
