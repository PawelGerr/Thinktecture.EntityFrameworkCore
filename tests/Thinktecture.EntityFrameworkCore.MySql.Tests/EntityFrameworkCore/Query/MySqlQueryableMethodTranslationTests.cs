namespace Thinktecture.EntityFrameworkCore.Query;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlQueryableMethodTranslationTests : MySqlIntegrationTestsBase
{
   public MySqlQueryableMethodTranslationTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_execute_a_subquery_when_bulk_operations_enable_the_custom_queryable_factory()
   {
      ArrangeDbContext.Entities.AddRange(
                                         new MySqlTestEntity { Id = 1, Name = "first" },
                                         new MySqlTestEntity { Id = 2, Name = "second" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.Entities
                                     .Select(entity => new { entity.Id, entity.Name })
                                     .AsSubQuery()
                                     .Where(entity => entity.Name == "second")
                                     .ToListAsync();

      result.Should().ContainSingle().Which.Id.Should().Be(2);
      ExecutedCommands.Should().Contain(command => command.Contains("FROM (", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_keep_fractional_seconds_of_a_constant_compared_with_a_temporal_column_with_explicit_precision()
   {
      // The columns are datetime(6) and timestamp(6): 123456 microseconds.
      var value = new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234560);
      ArrangeDbContext.TemporalEntities.Add(new MySqlTemporalEntity { Id = Guid.NewGuid(), DateTime = value, NullableDateTime = value, Converted = new MySqlDateTimeWrapper(value) });
      await ArrangeDbContext.SaveChangesAsync();

      var count = await ActDbContext.TemporalEntities.CountAsync(entity => entity.DateTime == EF.Constant(value) && entity.NullableDateTime == EF.Constant((DateTime?)value));

      count.Should().Be(1);
      ExecutedCommands.Should().Contain(command => command.Contains("'2024-05-06 07:08:09.123456'", StringComparison.Ordinal));
   }
}
