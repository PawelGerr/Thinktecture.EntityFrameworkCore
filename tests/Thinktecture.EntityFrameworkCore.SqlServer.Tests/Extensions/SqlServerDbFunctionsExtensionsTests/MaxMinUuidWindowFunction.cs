using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqlServerDbFunctionsExtensionsTests;

// Verifies windowed MAX/MIN over a uniqueidentifier column works natively on SQL Server
// (unlike PostgreSQL, SQL Server has a native MAX/MIN aggregate over uniqueidentifier, so no rewrite is needed).
// ReSharper disable once InconsistentNaming
public class MaxMinUuidWindowFunction : IntegrationTestsBase
{
   // Uniform-byte GUIDs so the expected max/min are unambiguous even under SQL Server's
   // uniqueidentifier comparison rules (which differ from byte/text ordering).
   private static readonly Guid _id1 = new("11111111-1111-1111-1111-111111111111");
   private static readonly Guid _id2 = new("22222222-2222-2222-2222-222222222222");
   private static readonly Guid _id3 = new("33333333-3333-3333-3333-333333333333");

   public MaxMinUuidWindowFunction(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public async Task Should_translate_windowed_Max_over_uuid()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "2", RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new { e.Id, e.Name, Max = EF.Functions.Max(e.Id, e.Name) })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Max.Should().Be(_id2));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Max.Should().Be(_id3));

      ExecutedCommands.Last().Should().Contain("MAX (").And.Contain("OVER (");
   }

   [Fact]
   public async Task Should_translate_windowed_Min_over_uuid()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "2", RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new { e.Id, e.Name, Min = EF.Functions.Min(e.Id, e.Name) })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Min.Should().Be(_id1));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Min.Should().Be(_id3));

      ExecutedCommands.Last().Should().Contain("MIN (").And.Contain("OVER (");
   }

   [Fact]
   public async Task Should_translate_windowed_Max_over_uuid_with_order_by()
   {
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "1", RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "2", RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new { e.Id, e.Name, Max = EF.Functions.Max(e.Id, e.Name, EF.Functions.OrderBy(e.Id)) })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      // Running max within each Name partition ordered by Id ascending.
      result.Single(t => t.Id == _id1).Max.Should().Be(_id1);
      result.Single(t => t.Id == _id2).Max.Should().Be(_id2);
      result.Single(t => t.Id == _id3).Max.Should().Be(_id3);

      ExecutedCommands.Last().Should().Contain("MAX (").And.Contain("OVER (");
   }
}
