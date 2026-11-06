using System.Transactions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTransactionScopeTests : MySqlIntegrationTestsBase
{
   public MySqlTransactionScopeTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_and_commit_inside_a_transaction_scope()
   {
      await using var context = await CreateContextAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Scoped" }]);
         scope.Complete();
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Scoped");
   }

   [Fact]
   public async Task Should_roll_back_an_insert_when_the_transaction_scope_is_not_completed()
   {
      await using var context = await CreateContextAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Scoped" }]);
      }

      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_upsert_inside_a_transaction_scope()
   {
      await using var context = await CreateContextAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertOrUpdateAsync(
            [new MySqlTestEntity { Id = 1, Name = "Scoped" }],
            propertiesToInsert: entity => new { entity.Id, entity.Name },
            propertiesToUpdate: entity => entity.Name,
            propertiesToMatchOn: entity => entity.Id);
         scope.Complete();
      }

      (await context.Entities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_update_inside_a_transaction_scope()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkUpdateAsync(
            [new MySqlTestEntity { Id = 1, Name = "After" }],
            entity => entity.Name,
            entity => entity.Id);
         scope.Complete();
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("After");
   }

   [Fact]
   public async Task Should_not_commit_a_truncate_when_the_transaction_scope_is_not_completed()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "One" }, new MySqlTestEntity { Id = 2, Name = "Two" }]);

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.TruncateTableAsync<MySqlTestEntity>();
      }

      (await context.Entities.CountAsync()).Should().Be(2);
   }
}
