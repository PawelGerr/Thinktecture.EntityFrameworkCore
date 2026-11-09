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

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_collection_with_null_element_and_relational_nulls()
   {
      Configure = builder => builder.UseSqlite(o => o.UseRelationalNulls());

      var withNull = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000008"), RequiredName = "RequiredName", NullableCount = null };
      var match = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000009"), RequiredName = "RequiredName", NullableCount = 7 };
      ArrangeDbContext.AddRange(withNull, match);
      await ArrangeDbContext.SaveChangesAsync();

      int?[] counts = [7, null];

      // Control: the SQL of EF Core for the same collection as a parameter.
      ActDbContext.TestEntities.Where(e => counts.Contains(e.NullableCount)).ToQueryString().Should().NotContain("IS NULL");

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => counts.Contains(s.NullableCount));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withNull.Id)).Count.Should().Be(0);
   }
}
