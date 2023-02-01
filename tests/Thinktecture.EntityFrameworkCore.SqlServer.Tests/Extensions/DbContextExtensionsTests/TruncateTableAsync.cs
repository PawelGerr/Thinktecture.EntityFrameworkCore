using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class TruncateTableAsync : IntegrationTestsBase
{
   public TruncateTableAsync(ITestOutputHelper testOutputHelper, SqlServerContainerFixture sqlServerContainerFixture)
      : base(testOutputHelper, sqlServerContainerFixture)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_table_is_empty()
   {
      await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<TestEntity>())
                        .Should().NotThrowAsync();
   }

   [Fact]
   public async Task Should_delete_entities()
   {
      ArrangeDbContext.Add(new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync<TestEntity>();

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }
}
