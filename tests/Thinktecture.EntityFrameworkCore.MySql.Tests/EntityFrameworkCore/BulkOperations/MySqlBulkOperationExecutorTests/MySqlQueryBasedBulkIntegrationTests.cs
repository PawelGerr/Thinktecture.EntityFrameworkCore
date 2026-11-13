namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlQueryBasedBulkIntegrationTests : MySqlIntegrationTestsBase
{
   public MySqlQueryBasedBulkIntegrationTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task BulkInsertFromQuery_Should_insert_mapped_columns()
   {
      await using var context = await CreateContextAsync();
      context.QuerySources.AddRange(
                                    new MySqlQuerySource { Id = 601, Name = "Source A", Count = 10 },
                                    new MySqlQuerySource { Id = 602, Name = "Source B", Count = 20 });
      await context.SaveChangesAsync();

      var affectedRows = await context.QueryTargets.BulkInsertAsync(
                                                                    context.QuerySources.AsNoTracking(),
                                                                    map => map.Map(target => target.Id, source => source.Id)
                                                                              .Map(target => target.Name, source => source.Name)
                                                                              .Map(target => target.Count, source => source.Count));

      affectedRows.Should().Be(2);
      var rows = await context.QueryTargets.AsNoTracking().OrderBy(target => target.Id).ToListAsync();
      rows.Select(row => row.Name).Should().Equal("Source A", "Source B");
      rows.Select(row => row.Count).Should().Equal(10, 20);
   }

   [Fact]
   public async Task BulkInsertFromQuery_Should_insert_constant_mapping()
   {
      await using var context = await CreateContextAsync();
      context.QuerySources.Add(new MySqlQuerySource { Id = 603, Name = "Source", Count = 10 });
      await context.SaveChangesAsync();

      await context.QueryTargets.BulkInsertAsync(
                                                 context.QuerySources.AsNoTracking(),
                                                 map => map.Map(target => target.Id, source => source.Id)
                                                           .Map(target => target.Name, source => "Mapped")
                                                           .Map(target => target.Count, source => source.Count));

      var row = await context.QueryTargets.AsNoTracking().SingleAsync();
      row.Name.Should().Be("Mapped");
      row.Count.Should().Be(10);
   }

   [Fact]
   public async Task BulkInsertFromQuery_Should_insert_expression_and_captured_value_mappings()
   {
      await using var context = await CreateContextAsync();
      const string prefix = "Mapped-";
      context.QuerySources.Add(new MySqlQuerySource { Id = 604, Name = "Source", Count = 10 });
      await context.SaveChangesAsync();

      await context.QueryTargets.BulkInsertAsync(
                                                 context.QuerySources.AsNoTracking(),
                                                 map => map.Map(target => target.Id, source => source.Id)
                                                           .Map(target => target.Name, source => prefix + source.Name)
                                                           .Map(target => target.Count, source => source.Count * 2 + 1));

      var row = await context.QueryTargets.AsNoTracking().SingleAsync();
      row.Name.Should().Be("Mapped-Source");
      row.Count.Should().Be(21);
   }

   [Fact]
   public async Task BulkUpdateFromQuery_Should_apply_join_and_filter()
   {
      ArrangeDbContext.QueryTargets.AddRange(
                                             new MySqlQueryTarget { Id = 611, Name = "Target A", Count = 5 },
                                             new MySqlQueryTarget { Id = 612, Name = "Target B", Count = 30 });
      ArrangeDbContext.QuerySources.AddRange(
                                             new MySqlQuerySource { Id = 611, Name = "Source A", Count = 10 },
                                             new MySqlQuerySource { Id = 612, Name = "Source B", Count = 20 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                                                                         ActDbContext.QuerySources.AsNoTracking(),
                                                                         target => target.Id,
                                                                         source => source.Id,
                                                                         setters => setters
                                                                                    .Set(target => target.Name, (target, source) => source.Name)
                                                                                    .Set(target => target.Count, (target, source) => source.Count),
                                                                         (target, source) => target.Count < source.Count);

      affectedRows.Should().Be(1);
      ExecutedCommands.Should().Contain(command => command.Contains("UPDATE `QueryTargets`", StringComparison.Ordinal)
                                                   && command.Contains("INNER JOIN", StringComparison.Ordinal)
                                                   && command.Contains("WHERE", StringComparison.Ordinal));
      var rows = await AssertDbContext.QueryTargets.AsNoTracking().OrderBy(target => target.Id).ToListAsync();
      rows[0].Name.Should().Be("Source A");
      rows[0].Count.Should().Be(10);
      rows[1].Name.Should().Be("Target B");
      rows[1].Count.Should().Be(30);
   }

   [Fact]
   public async Task BulkUpdateFromQuery_Should_apply_arithmetic_to_matching_rows()
   {
      await using var context = await CreateContextAsync();
      context.QueryTargets.Add(new MySqlQueryTarget { Id = 621, Name = "Target", Count = 2 });
      context.QuerySources.Add(new MySqlQuerySource { Id = 621, Name = "Source", Count = 5 });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.QueryTargets.BulkUpdateAsync(
                                                                    context.QuerySources.AsNoTracking(),
                                                                    target => target.Id,
                                                                    source => source.Id,
                                                                    setters => setters.Set(target => target.Count, (target, source) => target.Count + source.Count),
                                                                    (target, source) => target.Count < source.Count);

      affectedRows.Should().Be(1);
      (await context.QueryTargets.AsNoTracking().Select(target => target.Count).SingleAsync()).Should().Be(7);
   }

   [Fact]
   public async Task BulkUpdateFromQuery_Should_apply_a_captured_value()
   {
      await using var context = await CreateContextAsync();
      context.QueryTargets.Add(new MySqlQueryTarget { Id = 622, Name = "Target", Count = 2 });
      context.QuerySources.Add(new MySqlQuerySource { Id = 622, Name = "Source", Count = 5 });
      await context.SaveChangesAsync();
      const string name = "Captured";

      var affectedRows = await context.QueryTargets.BulkUpdateAsync(
                                                                    context.QuerySources.AsNoTracking(),
                                                                    target => target.Id,
                                                                    source => source.Id,
                                                                    setters => setters.Set(target => target.Name, (_, _) => name));

      affectedRows.Should().Be(1);
      (await context.QueryTargets.AsNoTracking().Select(target => target.Name).SingleAsync()).Should().Be(name);
   }

   [Fact]
   public async Task BulkUpdateFromQuery_Should_return_zero_when_no_key_matches()
   {
      await using var context = await CreateContextAsync();
      context.QueryTargets.Add(new MySqlQueryTarget { Id = 631, Name = "Target", Count = 2 });
      context.QuerySources.Add(new MySqlQuerySource { Id = 999, Name = "Source", Count = 5 });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.QueryTargets.BulkUpdateAsync(
                                                                    context.QuerySources.AsNoTracking(),
                                                                    target => target.Id,
                                                                    source => source.Id,
                                                                    setters => setters.Set(target => target.Name, (target, source) => source.Name));

      affectedRows.Should().Be(0);
      (await context.QueryTargets.AsNoTracking().Select(target => target.Name).SingleAsync()).Should().Be("Target");
   }
}
