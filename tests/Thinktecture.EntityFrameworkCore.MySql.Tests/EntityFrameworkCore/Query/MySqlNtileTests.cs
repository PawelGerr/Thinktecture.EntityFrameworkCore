namespace Thinktecture.EntityFrameworkCore.Query;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlNtileTests : MySqlIntegrationTestsBase
{
   public MySqlNtileTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_apply_partitions_when_executing_ntile()
   {
      ArrangeDbContext.QuerySources.AddRange(
                                             new MySqlQuerySource { Id = 1, Name = "A", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = "A", Count = 2 },
                                             new MySqlQuerySource { Id = 3, Name = "A", Count = 3 },
                                             new MySqlQuerySource { Id = 4, Name = "B", Count = 1 },
                                             new MySqlQuerySource { Id = 5, Name = "B", Count = 2 },
                                             new MySqlQuerySource { Id = 6, Name = "B", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.QuerySources
                                     .OrderBy(entity => entity.Id)
                                     .Select(entity => new
                                                       {
                                                          entity.Name,
                                                          Bucket = EF.Functions.NTile(2, entity.Name, EF.Functions.OrderBy(entity.Count))
                                                       })
                                     .ToListAsync();

      result.Where(item => item.Name == "A").Select(item => item.Bucket).Should().Equal(1, 1, 2);
      result.Where(item => item.Name == "B").Select(item => item.Bucket).Should().Equal(1, 1, 2);
   }
}
