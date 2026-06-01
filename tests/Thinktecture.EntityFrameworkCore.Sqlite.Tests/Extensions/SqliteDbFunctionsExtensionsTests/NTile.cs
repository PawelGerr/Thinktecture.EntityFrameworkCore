using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqliteDbFunctionsExtensionsTests;

// ReSharper disable once InconsistentNaming
public class NTile : IntegrationTestsBase
{
   public NTile(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
      : base(testOutputHelper, providerFactoryFixture)
   {
   }

   [Fact]
   public void Generates_NTile_with_orderby_and_two_buckets()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Bucket = EF.Functions.NTile(2, EF.Functions.OrderBy(e.Name))
                                            })
                               .ToList();

      result.First(t => t.Name == "1").Bucket.Should().Be(1);
      result.First(t => t.Name == "2").Bucket.Should().Be(2);
   }

   [Fact]
   public void Generates_NTile_with_orderby_desc()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Bucket = EF.Functions.NTile(2, EF.Functions.OrderByDescending(e.Name))
                                            })
                               .ToList();

      result.First(t => t.Name == "1").Bucket.Should().Be(2);
      result.First(t => t.Name == "2").Bucket.Should().Be(1);
   }

   [Fact]
   public void Generates_NTile_with_partitionby_and_orderby()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "A", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "A", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("28C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "B", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("38C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "B", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Bucket = EF.Functions.NTile(2, e.Name, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.First(t => t.Name == "A" && t.Count == 1).Bucket.Should().Be(1);
      result.First(t => t.Name == "A" && t.Count == 2).Bucket.Should().Be(2);
      result.First(t => t.Name == "B" && t.Count == 1).Bucket.Should().Be(1);
      result.First(t => t.Name == "B" && t.Count == 2).Bucket.Should().Be(2);
   }

   [Fact]
   public void Generates_NTile_with_bucket_count_from_variable()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var bucketCount = 2;

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e.Name,
                                              Bucket = EF.Functions.NTile(bucketCount, EF.Functions.OrderBy(e.Name))
                                           });

      query.ToQueryString().Should().Contain("NTILE");

      var result = query.ToList();
      result.First(t => t.Name == "1").Bucket.Should().Be(1);
      result.First(t => t.Name == "2").Bucket.Should().Be(2);
   }

   [Fact]
   public void Generates_NTile_without_orderby()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Bucket = EF.Functions.NTile(2)
                                            })
                               .ToList();

      result.Should().HaveCount(2);
      result.Select(t => t.Bucket).OrderBy(b => b).Should().Equal(1, 2);
   }
}
