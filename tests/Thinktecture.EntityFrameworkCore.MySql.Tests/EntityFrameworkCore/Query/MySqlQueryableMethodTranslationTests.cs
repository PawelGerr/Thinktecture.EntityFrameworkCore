namespace Thinktecture.EntityFrameworkCore.Query;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlQueryableMethodTranslationTests : MySqlIntegrationTestsBase
{
   public MySqlQueryableMethodTranslationTests(MySqlContainerFixture fixture)
      : base(fixture)
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
}
