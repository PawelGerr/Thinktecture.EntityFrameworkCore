using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

public class ToComplexCollectionParameter : IntegrationTestsBase
{
   public ToComplexCollectionParameter(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
   {
   }

   [Fact]
   public void Should_do_roundtrip()
   {
      ActDbContext.ToComplexCollectionParameter(new[] { new MyParameter(new Guid("D3E99F44-40A1-4E4E-820F-9D7C7B02AFA5"), new ConvertibleClass(42)) })
                  .ToList()
                  .Should().BeEquivalentTo(new[] { new MyParameter(new Guid("D3E99F44-40A1-4E4E-820F-9D7C7B02AFA5"), new ConvertibleClass(42)) });
   }

   [Fact]
   public async Task Should_work_with_joins()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                          Name = "Name",
                          ConvertibleClass = new ConvertibleClass(42)
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.ToComplexCollectionParameter(new[] { new MyParameter(testEntity.Id, new ConvertibleClass(42)) });
      var loadedEntities = await ActDbContext.TestEntities
                                             .Join(collectionParameter, t => new { t.Id, ConvertibleClass = t.ConvertibleClass! }, p => new { Id = p.Column1, ConvertibleClass = p.Column2 }, (t, p) => t)
                                             .ToListAsync();

      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                                              Name = "Name",
                                              ConvertibleClass = new ConvertibleClass(42)
                                           });
   }
}
