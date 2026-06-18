using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Query;

// Verifies SQLite translates max/min over a Guid column natively (no provider-side rewrite needed).
// ReSharper disable once InconsistentNaming
public class MaxMinUuidAggregate : IntegrationTestsBase
{
   // Uniform-byte GUIDs so the expected max/min are unambiguous under any comparison rule.
   private static readonly Guid _id1 = new("11111111-1111-1111-1111-111111111111");
   private static readonly Guid _id2 = new("22222222-2222-2222-2222-222222222222");
   private static readonly Guid _id3 = new("33333333-3333-3333-3333-333333333333");
   private static readonly Guid _id4 = new("44444444-4444-4444-4444-444444444444");

   public MaxMinUuidAggregate(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
      : base(testOutputHelper, providerFactoryFixture)
   {
   }

   [Fact]
   public async Task Should_translate_Max_over_uuid_grouping()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id3, Count = 2, RequiredName = "R" },
         new TestEntity { Id = _id4, Count = 2, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Count)
                                     .Select(g => new { Count = g.Key, MaxId = g.Max(e => e.Id) })
                                     .OrderBy(x => x.Count)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].MaxId.Should().Be(_id2);
      result[1].MaxId.Should().Be(_id4);
   }

   [Fact]
   public async Task Should_translate_Min_over_uuid_grouping()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id3, Count = 2, RequiredName = "R" },
         new TestEntity { Id = _id4, Count = 2, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Count)
                                     .Select(g => new { Count = g.Key, MinId = g.Min(e => e.Id) })
                                     .OrderBy(x => x.Count)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].MinId.Should().Be(_id1);
      result[1].MinId.Should().Be(_id3);
   }

   [Fact]
   public async Task Should_translate_Max_over_nullable_uuid()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Count = 0, RequiredName = "R" },
         new TestEntity { Id = _id2, Count = 0, RequiredName = "R" });
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id3, Count = 7, ParentId = null, RequiredName = "R" },
         new TestEntity { Id = _id4, Count = 7, ParentId = _id1, RequiredName = "R" },
         new TestEntity { Id = new("55555555-5555-5555-5555-555555555555"), Count = 7, ParentId = _id2, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .Where(e => e.Count == 7)
                                     .GroupBy(e => e.Count)
                                     .Select(g => g.Max(e => e.ParentId))
                                     .ToListAsync();

      result.Should().ContainSingle();
      result[0].Should().Be(_id2);
   }

   [Fact]
   public async Task Should_translate_ntile_groupby_max_with_having_to_partition_boundaries()
   {
      var rangeCount = 2;

      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, RequiredName = "R" },
         new TestEntity { Id = _id2, RequiredName = "R" },
         new TestEntity { Id = _id3, RequiredName = "R" },
         new TestEntity { Id = _id4, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var boundaries = await ActDbContext.TestEntities
                                         .Select(b => new
                                                      {
                                                         b.Id,
                                                         Bucket = EF.Functions.NTile(rangeCount, EF.Functions.OrderBy(b.Id))
                                                      })
                                         .AsSubQuery()
                                         .GroupBy(x => x.Bucket)
                                         .Where(g => g.Key < rangeCount)
                                         .OrderBy(g => g.Key)
                                         .Select(g => g.Max(x => x.Id))
                                         .ToListAsync();

      // HAVING bucket < 2 keeps only bucket 1 = {_id1, _id2}; its boundary (max) is _id2.
      boundaries.Should().ContainSingle();
      boundaries[0].Should().Be(_id2);

      var sql = ExecutedCommands.Last();
      sql.Should().Contain("NTILE")
         .And.Contain("OVER (")
         .And.Contain("MAX(")
         .And.Contain("GROUP BY")
         .And.Contain("HAVING")
         .And.Contain("ORDER BY");
   }
}
