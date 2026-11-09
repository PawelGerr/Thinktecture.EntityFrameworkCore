using Microsoft.EntityFrameworkCore.Diagnostics;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertFromQueryWithConvertedColumnsAsync : SchemaChangingIntegrationTestsBase
{
   public BulkInsertFromQueryWithConvertedColumnsAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_insert_value_converted_int_property_from_server_conditional_over_constants()
   {
      ConfigureModel = builder => builder.Entity<TestEntity>().Property(e => e.Count).HasConversion(v => v * 100, v => v / 100);
      Configure = builder => builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

      var source = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000003"), RequiredName = "RequiredName", NullableCount = 20 };
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("8D000000-0000-0000-0000-000000000004");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => s.Id == source.Id),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.NullableCount > 15 ? 2 : 1)
                           .Map(e => e.PropertyWithBackingField, s => s.PropertyWithBackingField)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => EF.Property<int>(s, "_privateField")));

      affectedRows.Should().Be(1);

      var stored = await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "Count" AS "Value" FROM "TestEntities" WHERE "Id" = '8D000000-0000-0000-0000-000000000004' """).SingleAsync();
      stored.Should().Be(200);
   }
}
