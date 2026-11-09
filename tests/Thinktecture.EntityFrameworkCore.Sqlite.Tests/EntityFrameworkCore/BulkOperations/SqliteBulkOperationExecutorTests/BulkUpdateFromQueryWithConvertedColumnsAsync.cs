using Microsoft.EntityFrameworkCore.Diagnostics;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryWithConvertedColumnsAsync : SchemaChangingIntegrationTestsBase
{
   public BulkUpdateFromQueryWithConvertedColumnsAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_update_value_converted_int_property_from_server_conditional_over_constants()
   {
      ConfigureModel = builder => builder.Entity<TestEntity>().Property(e => e.Count).HasConversion(v => v * 100, v => v / 100);
      Configure = builder => builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

      var low = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000001"), RequiredName = "RequiredName", NullableCount = 10 };
      var high = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000002"), RequiredName = "RequiredName", NullableCount = 20 };
      ArrangeDbContext.AddRange(low, high);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => s.NullableCount > 15 ? 2 : 1));

      affectedRows.Should().Be(2);

      var stored = await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "Count" AS "Value" FROM "TestEntities" ORDER BY "NullableCount" """).ToListAsync();
      stored.Should().Equal(100, 200);
   }
}
