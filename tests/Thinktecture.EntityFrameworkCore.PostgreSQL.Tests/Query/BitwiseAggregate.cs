using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Query;

// ReSharper disable once InconsistentNaming
public class BitwiseAggregate : IntegrationTestsBase
{
   private static readonly Guid _id1 = new("11111111-1111-1111-1111-111111111111");
   private static readonly Guid _id2 = new("22222222-2222-2222-2222-222222222222");
   private static readonly Guid _id3 = new("33333333-3333-3333-3333-333333333333");
   private static readonly Guid _id4 = new("44444444-4444-4444-4444-444444444444");

   public BitwiseAggregate(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_translate_BitOr_over_int_grouping()
   {
      // Group "A": 0b001 | 0b010 = 0b011 = 3 ; Group "B": 0b110 | 0b011 = 0b111 = 7
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "A", Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "A", Count = 2, RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "B", Count = 6, RequiredName = "R" },
         new TestEntity { Id = _id4, Name = "B", Count = 3, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Name)
                                     .Select(g => new { Name = g.Key, Flags = EF.Functions.BitOr(g.Select(e => e.Count)) })
                                     .OrderBy(x => x.Name)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].Flags.Should().Be(3);
      result[1].Flags.Should().Be(7);

      ExecutedCommands.Last().Should().Contain("bit_or");
   }

   [Fact]
   public async Task Should_translate_BitAnd_over_int_grouping()
   {
      // Group "A": 0b001 & 0b010 = 0 ; Group "B": 0b110 & 0b011 = 0b010 = 2
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "A", Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "A", Count = 2, RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "B", Count = 6, RequiredName = "R" },
         new TestEntity { Id = _id4, Name = "B", Count = 3, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Name)
                                     .Select(g => new { Name = g.Key, Flags = EF.Functions.BitAnd(g.Select(e => e.Count)) })
                                     .OrderBy(x => x.Name)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].Flags.Should().Be(0);
      result[1].Flags.Should().Be(2);

      ExecutedCommands.Last().Should().Contain("bit_and");
   }

   [Fact]
   public async Task Should_translate_BitXor_over_int_grouping()
   {
      // Group "A": 0b001 ^ 0b010 = 0b011 = 3 ; Group "B": 0b110 ^ 0b011 = 0b101 = 5
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "A", Count = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "A", Count = 2, RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "B", Count = 6, RequiredName = "R" },
         new TestEntity { Id = _id4, Name = "B", Count = 3, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Name)
                                     .Select(g => new { Name = g.Key, Flags = EF.Functions.BitXor(g.Select(e => e.Count)) })
                                     .OrderBy(x => x.Name)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].Flags.Should().Be(3);
      result[1].Flags.Should().Be(5);

      ExecutedCommands.Last().Should().Contain("bit_xor");
   }

   [Fact]
   public async Task Should_translate_BitOr_over_nullable_int_ignoring_nulls()
   {
      // NULLs are ignored by bit_or: 1 | null | 2 = 3
      ArrangeDbContext.TestEntities.AddRange(
         new TestEntity { Id = _id1, Name = "A", Count = 0, NullableCount = 1, RequiredName = "R" },
         new TestEntity { Id = _id2, Name = "A", Count = 0, NullableCount = null, RequiredName = "R" },
         new TestEntity { Id = _id3, Name = "A", Count = 0, NullableCount = 2, RequiredName = "R" });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntities
                                     .GroupBy(e => e.Name)
                                     .Select(g => EF.Functions.BitOr(g.Select(e => e.NullableCount)))
                                     .ToListAsync();

      result.Should().ContainSingle();
      result[0].Should().Be(3);

      ExecutedCommands.Last().Should().Contain("bit_or");
   }

   [Fact]
   public async Task Should_translate_BitOr_over_flags_enum_grouping()
   {
      // Group 1: A | B = (1 | 2) = 3 ; Group 2: A | C = (1 | 4) = 5
      ArrangeDbContext.TestEntitiesWithFlags.AddRange(
         new TestEntityWithFlags { Id = _id1, GroupId = 1, Phase = PhaseMembership.A },
         new TestEntityWithFlags { Id = _id2, GroupId = 1, Phase = PhaseMembership.B },
         new TestEntityWithFlags { Id = _id3, GroupId = 2, Phase = PhaseMembership.A },
         new TestEntityWithFlags { Id = _id4, GroupId = 2, Phase = PhaseMembership.C });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TestEntitiesWithFlags
                                     .GroupBy(e => e.GroupId)
                                     .Select(g => new { GroupId = g.Key, Phases = EF.Functions.BitOr(g.Select(e => e.Phase)) })
                                     .OrderBy(x => x.GroupId)
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result[0].Phases.Should().Be(PhaseMembership.A | PhaseMembership.B);
      result[1].Phases.Should().Be(PhaseMembership.A | PhaseMembership.C);

      ExecutedCommands.Last().Should().Contain("bit_or");
   }
}
