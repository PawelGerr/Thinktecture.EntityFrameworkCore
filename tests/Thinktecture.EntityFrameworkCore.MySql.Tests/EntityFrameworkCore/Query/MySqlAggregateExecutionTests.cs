namespace Thinktecture.EntityFrameworkCore.Query;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlAggregateExecutionTests : MySqlIntegrationTestsBase
{
   public MySqlAggregateExecutionTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_aggregate_bitwise_over_multiple_rows()
   {
      await using var context = await SeedAsync(1, 2, 4);

      var result = await context.Entities
                                .GroupBy(entity => entity.Id < 100)
                                .Select(group => new
                                                 {
                                                    Or = EF.Functions.BitOr(group.Select(entity => entity.Id)),
                                                    And = EF.Functions.BitAnd(group.Select(entity => entity.Id)),
                                                    Xor = EF.Functions.BitXor(group.Select(entity => entity.Id))
                                                 })
                                .SingleAsync();

      result.Or.Should().Be(7);
      result.And.Should().Be(0);
      result.Xor.Should().Be(7);
   }

   [Fact]
   public async Task Should_read_negative_result_into_int()
   {
      await using var context = await SeedAsync(-1);

      var result = await context.Entities
                                .GroupBy(entity => entity.Id < 100)
                                .Select(group => new
                                                 {
                                                    Or = EF.Functions.BitOr(group.Select(entity => entity.Id)),
                                                    And = EF.Functions.BitAnd(group.Select(entity => entity.Id))
                                                 })
                                .SingleAsync();

      result.Or.Should().Be(-1);
      result.And.Should().Be(-1);
   }

   [Fact]
   public async Task Should_read_negative_result_into_long()
   {
      await using var context = await SeedAsync(-1);

      var result = await context.Entities
                                .GroupBy(entity => entity.Id < 100)
                                .Select(group => EF.Functions.BitOr(group.Select(entity => (long)entity.Id)))
                                .SingleAsync();

      result.Should().Be(-1L);
   }

   [Fact]
   public async Task Should_return_null_for_group_without_matching_rows()
   {
      await using var context = await SeedAsync(1, 2);

      var result = await context.Entities
                                .GroupBy(entity => entity.Id < 100)
                                .Select(group => new
                                                 {
                                                    Or = EF.Functions.BitOr(group.Where(entity => entity.Id > 100).Select(entity => (int?)entity.Id)),
                                                    And = EF.Functions.BitAnd(group.Where(entity => entity.Id > 100).Select(entity => (int?)entity.Id))
                                                 })
                                .SingleAsync();

      result.Or.Should().BeNull();
      result.And.Should().BeNull();
   }

   [Fact]
   public async Task Should_execute_bit_or_with_distinct()
   {
      await using var context = await SeedAsync(1, 3, 4);

      var result = await context.Entities
                                .GroupBy(entity => entity.Id < 100)
                                .Select(group => EF.Functions.BitOr(group.Select(entity => entity.Id % 2).Distinct()))
                                .SingleAsync();

      result.Should().Be(1);
   }

   private async Task<MySqlTestDbContext> SeedAsync(params int[] ids)
   {
      var context = await CreateContextAsync();
      context.Entities.AddRange(ids.Select(id => new MySqlTestEntity { Id = id, Name = $"n{id}" }));
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();
      return context;
   }
}
