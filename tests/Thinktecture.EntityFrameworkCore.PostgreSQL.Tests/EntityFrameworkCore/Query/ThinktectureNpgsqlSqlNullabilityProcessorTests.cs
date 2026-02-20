using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Query;

public class ThinktectureNpgsqlSqlNullabilityProcessorTests : IntegrationTestsBase
{
   public ThinktectureNpgsqlSqlNullabilityProcessorTests(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_handle_HashSet_Contains_translated_to_PgAnyExpression()
   {
      var entityId = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA");
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = entityId, Name = "Test", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      var ids = new HashSet<Guid> { entityId };

      var result = await ActDbContext.TestEntities
                                     .Where(e => ids.Contains(e.Id))
                                     .ToListAsync();

      result.Should().HaveCount(1);
      result[0].Id.Should().Be(entityId);
   }
}
