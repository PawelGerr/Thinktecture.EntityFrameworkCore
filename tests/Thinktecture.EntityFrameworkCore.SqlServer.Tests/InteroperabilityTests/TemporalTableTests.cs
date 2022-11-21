using Thinktecture.EntityFrameworkCore.Testing;

namespace Thinktecture.InteroperabilityTests;

public class TemporalTableTests : IntegrationTestsBase
{
   public TemporalTableTests(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, ITestIsolationOptions.SharedTablesAmbientTransaction)
   {
   }

   [Fact]
   public async Task Should_not_conflict_with_other_components()
   {
      var query = ActDbContext.TestTemporalTableEntity
                              .TemporalBetween(new DateTime(2021, 12, 1), new DateTime(2021, 12, 15));

      await query.Awaiting(q => q.ToListAsync()).Should().NotThrowAsync();
   }
}
