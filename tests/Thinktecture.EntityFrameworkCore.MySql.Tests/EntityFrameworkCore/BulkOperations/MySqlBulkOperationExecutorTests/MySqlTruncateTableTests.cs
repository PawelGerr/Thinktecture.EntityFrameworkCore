using System.Transactions;
using MySqlConnector;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTruncateTableTests : MySqlIntegrationTestsBase
{
   public MySqlTruncateTableTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_remove_all_rows()
   {
      await using var context = await CreateContextAsync();
      context.QuerySources.AddRange(new MySqlQuerySource { Id = 401, Name = "One" }, new MySqlQuerySource { Id = 402, Name = "Two" });
      await context.SaveChangesAsync();

      await context.TruncateTableAsync<MySqlQuerySource>();

      (await context.QuerySources.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_truncate_table_whose_name_contains_braces()
   {
      ConfigureModel = builder => builder.Entity<BraceNamedEntity>().ToTable(BraceNamedEntity.TABLE_NAME);

      // ExecuteSqlRawAsync formats the SQL, so the braces are doubled here.
      await ArrangeDbContext.Database.ExecuteSqlRawAsync("CREATE TABLE `truncate{{target}}` (`Id` int NOT NULL PRIMARY KEY)");

      try
      {
         await ArrangeDbContext.Database.ExecuteSqlRawAsync("INSERT INTO `truncate{{target}}` (`Id`) VALUES (1), (2)");

         await ActDbContext.TruncateTableAsync<BraceNamedEntity>();

         (await AssertDbContext.Set<BraceNamedEntity>().CountAsync()).Should().Be(0);
      }
      finally
      {
         await ArrangeDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `truncate{{target}}`");
      }
   }

   [Fact]
   public async Task Should_throw_if_table_is_referenced_by_a_foreign_key()
   {
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 410, Name = "Parent" });
      await ArrangeDbContext.SaveChangesAsync();

      // InnoDB rejects TRUNCATE of a table that a foreign key references, also when the child table is empty.
      (await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<MySqlTestEntity>())
                         .Should().ThrowAsync<MySqlException>())
         .Which.Number.Should().Be(1701);

      (await AssertDbContext.Entities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_if_a_transaction_is_active()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await using (var transaction = await ActDbContext.Database.BeginTransactionAsync())
      {
         await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<MySqlQuerySource>())
                           .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");

         await transaction.RollbackAsync();
      }

      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_if_the_context_is_enlisted_in_a_transaction()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      using (var transaction = new CommittableTransaction())
      {
         // MySqlConnection.EnlistTransaction throws "Connection is not open." for a closed connection.
         await ActDbContext.Database.OpenConnectionAsync();
         ActDbContext.Database.EnlistTransaction(transaction);

         await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<MySqlQuerySource>())
                           .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");

         transaction.Rollback();
      }

      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_if_the_context_uses_an_external_transaction()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.OpenConnectionAsync();

      await using (var transaction = await ActDbContext.Database.GetDbConnection().BeginTransactionAsync())
      {
         await ActDbContext.Database.UseTransactionAsync(transaction);

         await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<MySqlQuerySource>())
                           .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");

         await transaction.RollbackAsync();
      }

      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_execute_TRUNCATE_TABLE_without_transaction()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync<MySqlQuerySource>();

      ExecutedCommands.Should().Contain(command => command.Contains("TRUNCATE TABLE `QuerySources`", StringComparison.Ordinal));
      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_truncate_and_commit_implicitly_if_throwOnTransaction_is_false()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await using (var transaction = await ActDbContext.Database.BeginTransactionAsync())
      {
         ActDbContext.Entities.Add(new MySqlTestEntity { Id = 420, Name = "Inside" });
         await ActDbContext.SaveChangesAsync();

         await ActDbContext.TruncateTableAsync<MySqlQuerySource>(throwOnTransaction: false);

         // TRUNCATE TABLE has committed the insert above, so the rollback has nothing to undo.
         await transaction.RollbackAsync();
      }

      ExecutedCommands.Should().Contain(command => command.Contains("TRUNCATE TABLE `QuerySources`", StringComparison.Ordinal));
      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(0);
      (await AssertDbContext.Entities.Select(entity => entity.Name).ToListAsync()).Should().Equal("Inside");
   }

   [Fact]
   public async Task Should_throw_if_throwOnTransaction_is_true_and_a_transaction_is_active()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await using (var transaction = await ActDbContext.Database.BeginTransactionAsync())
      {
         await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync<MySqlQuerySource>(throwOnTransaction: true))
                           .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");

         await transaction.RollbackAsync();
      }

      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_truncate_without_transaction_if_throwOnTransaction_is_true()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync<MySqlQuerySource>(throwOnTransaction: true);

      ExecutedCommands.Should().Contain(command => command.Contains("TRUNCATE TABLE `QuerySources`", StringComparison.Ordinal));
      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_if_throwOnTransaction_is_true_and_a_transaction_is_active_using_the_type()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await using (var transaction = await ActDbContext.Database.BeginTransactionAsync())
      {
         await ActDbContext.Awaiting(ctx => ctx.TruncateTableAsync(typeof(MySqlQuerySource), throwOnTransaction: true))
                           .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");

         await transaction.RollbackAsync();
      }

      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_truncate_without_transaction_if_throwOnTransaction_is_true_using_the_type()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TruncateTableAsync(typeof(MySqlQuerySource), throwOnTransaction: true);

      ExecutedCommands.Should().Contain(command => command.Contains("TRUNCATE TABLE `QuerySources`", StringComparison.Ordinal));
      (await AssertDbContext.QuerySources.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_truncate_an_empty_table()
   {
      await using var context = await CreateContextAsync();

      await context.TruncateTableAsync<MySqlQuerySource>();

      (await context.QuerySources.CountAsync()).Should().Be(0);
   }

   private sealed class BraceNamedEntity
   {
      public const string TABLE_NAME = "truncate{target}";

      public int Id { get; set; }
   }
}
