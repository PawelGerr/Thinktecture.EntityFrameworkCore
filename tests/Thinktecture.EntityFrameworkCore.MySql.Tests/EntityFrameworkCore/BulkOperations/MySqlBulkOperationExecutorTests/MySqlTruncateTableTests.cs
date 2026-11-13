namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTruncateTableTests : MySqlIntegrationTestsBase
{
   public MySqlTruncateTableTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_remove_all_rows()
   {
      await using var context = await CreateContextAsync();
      context.Entities.AddRange(new MySqlTestEntity { Id = 401, Name = "One" }, new MySqlTestEntity { Id = 402, Name = "Two" });
      await context.SaveChangesAsync();

      await context.TruncateTableAsync<MySqlTestEntity>();

      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_truncate_a_table_referenced_by_a_foreign_key()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 410, Name = "Parent" });
      await context.SaveChangesAsync();

      await context.TruncateTableAsync<MySqlTestEntity>();

      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_commit_the_surrounding_transaction()
   {
      await using var context = await CreateContextAsync();
      context.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await context.SaveChangesAsync();

      await using (var transaction = await context.Database.BeginTransactionAsync())
      {
         context.Entities.Add(new MySqlTestEntity { Id = 420, Name = "Inside" });
         await context.SaveChangesAsync();

         await context.TruncateTableAsync<MySqlQuerySource>();

         await transaction.RollbackAsync();
      }

      context.ChangeTracker.Clear();
      (await context.Entities.CountAsync()).Should().Be(0);
      (await context.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_use_TRUNCATE_for_a_table_that_is_not_referenced_by_a_foreign_key()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync<MySqlQuerySource>();

      ExecutedCommands.Should().Contain(command => command.Contains("TRUNCATE TABLE `QuerySources`", StringComparison.Ordinal));
      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_use_DELETE_for_a_table_referenced_by_a_foreign_key()
   {
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 430, Name = "Parent" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync<MySqlTestEntity>();

      ExecutedCommands.Should().Contain(command => command.Contains("DELETE FROM `Entities`", StringComparison.Ordinal));
      ExecutedCommands.Should().NotContain(command => command.Contains("TRUNCATE TABLE", StringComparison.Ordinal));
      (await AssertDbContext.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_use_DELETE_inside_a_transaction()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await using (var transaction = await ActDbContext.Database.BeginTransactionAsync())
      {
         await ActDbContext.TruncateTableAsync<MySqlQuerySource>();
         await transaction.CommitAsync();
      }

      ExecutedCommands.Should().Contain(command => command.Contains("DELETE FROM `QuerySources`", StringComparison.Ordinal));
      ExecutedCommands.Should().NotContain(command => command.Contains("TRUNCATE TABLE", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_truncate_an_empty_table()
   {
      await using var context = await CreateContextAsync();

      await context.TruncateTableAsync<MySqlTestEntity>();

      (await context.Entities.CountAsync()).Should().Be(0);
   }
}
