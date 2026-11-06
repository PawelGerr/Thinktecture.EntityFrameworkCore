using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.TestDatabaseContext;
using IsolationLevel = System.Data.IsolationLevel;

namespace Thinktecture.EntityFrameworkCore.Storage;

// Ported from the PostgreSQL/SQL Server tests. Besides plain SaveChanges, the bulk operations (which write with raw ADO commands) have to take part in the nested transactions as well.
// MySqlNestedTransactionTests contains the smaller, MySQL-specific scenarios.
[Collection(MySqlTestCollection.NAME)]
public sealed class NestedTransactionTests : MySqlIntegrationTestsBase
{
   private static readonly Guid _FIRST_ID = new("7631FF3B-E5B1-485A-ABD5-386C6B37E991");
   private static readonly Guid _SECOND_ID = new("7A4397B0-C9B6-46FF-B8C8-F39A9D6F5105");
   private static readonly Guid _THIRD_ID = new("CB682609-1503-4FBE-8A29-66E2DF8F00E5");

   private NestedRelationalTransactionManager SUT => field ??= (NestedRelationalTransactionManager)ActDbContext.GetService<IDbContextTransactionManager>();

   public NestedTransactionTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_create_new_root_transaction()
   {
      SUT.CurrentTransaction.Should().BeNull();

      await using var tx = await SUT.BeginTransactionAsync();

      tx.Should().NotBeNull();
      tx.Should().Be(SUT.CurrentTransaction);
      tx.GetDbTransaction().Should().NotBeNull();
      (await IsTransactionUsableAsync(tx.GetDbTransaction())).Should().BeTrue();
   }

   [Fact]
   public async Task Should_create_new_root_transaction_and_commit_correctly()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await tx.CommitAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_create_new_root_transaction_and_rollback_correctly()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await tx.RollbackAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_create_new_root_transaction_and_dispose_correctly()
   {
      // Disposing a transaction that is neither committed nor rolled back rolls it back.
      await using (await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_create_new_child_transaction()
   {
      await using var tx = await SUT.BeginTransactionAsync();
      await using var childTx = await SUT.BeginTransactionAsync();

      childTx.Should().NotBeNull();
      childTx.Should().Be(SUT.CurrentTransaction);
      childTx.GetDbTransaction().Should().NotBeNull();
      childTx.GetDbTransaction().Should().Be(tx.GetDbTransaction());
      (await IsTransactionUsableAsync(childTx.GetDbTransaction())).Should().BeTrue();
   }

   [Fact]
   public async Task Should_create_new_child_transaction_and_commit_correctly()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      await using (var child = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await child.CommitAsync();
         await tx.CommitAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_create_new_child_transaction_and_rollback_correctly()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      await using (var childTx = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await childTx.RollbackAsync();
         await tx.RollbackAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_create_new_child_transaction_and_dispose_correctly()
   {
      await using (await SUT.BeginTransactionAsync())
      await using (await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_complete_underlying_translations_after_root_is_completed()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      await using (var child = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { Id = _FIRST_ID, RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await child.CommitAsync();
         ActDbContext.TestEntities.Add(new TestEntity { Id = _SECOND_ID, RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await tx.RollbackAsync();

         // outside of the transaction
         ActDbContext.TestEntities.Add(new TestEntity { Id = _THIRD_ID, RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();
      }

      var ids = await AssertDbContext.TestEntities.Select(e => e.Id).ToListAsync();
      ids.Should().Equal(_THIRD_ID);
   }

   [Fact]
   public async Task Should_rollback_if_a_sibling_transaction_rollbacks()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         await using (var child = await SUT.BeginTransactionAsync())
         {
            await child.RollbackAsync();
         }

         await using (var child = await SUT.BeginTransactionAsync())
         {
            await child.CommitAsync();
         }

         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await tx.Awaiting(rootTx => rootTx.CommitAsync())
                 .Should().ThrowAsync<TransactionAbortedException>().WithMessage("The transaction has aborted.");
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_dispose_child_transactions_if_root_transactions_is_disposed()
   {
      await using (await SUT.BeginTransactionAsync())
      {
         // The child is intentionally neither completed nor disposed.
         await SUT.BeginTransactionAsync();

         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();
      }

      SUT.CurrentTransaction.Should().BeNull();
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_dispose_inner_child_transactions_if_outer_transactions_is_disposed()
   {
      await using (await SUT.BeginTransactionAsync())
      {
         var child = await SUT.BeginTransactionAsync();

         // The inner child is intentionally neither completed nor disposed.
         await SUT.BeginTransactionAsync();

         ActDbContext.TestEntities.Add(new TestEntity { RequiredName = "RequiredName" });
         await ActDbContext.SaveChangesAsync();

         await child.DisposeAsync();
      }

      SUT.CurrentTransaction.Should().BeNull();
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_allow_create_child_transactions_with_higher_level()
   {
      await using var rootTx = await SUT.BeginTransactionAsync(IsolationLevel.RepeatableRead);

      rootTx.GetDbTransaction().IsolationLevel.Should().Be(IsolationLevel.RepeatableRead);

      await SUT.Awaiting(sut => sut.BeginTransactionAsync(IsolationLevel.Serializable))
               .Should().ThrowAsync<InvalidOperationException>().WithMessage("The isolation level 'RepeatableRead' of the parent transaction is not compatible to the provided isolation level 'Serializable'.");
   }

   // MySQL's default isolation level is REPEATABLE READ, so a child asking for REPEATABLE READ or a weaker level is compatible.
   // MySqlConnector reports IsolationLevel.Unspecified for a transaction that was started without a level, and the nested
   // transaction rejects every child level below a parent reporting Unspecified (NestedDbContextTransaction.EnsureIsolationLevelCompatible).
   [Theory]
   [InlineData(IsolationLevel.ReadCommitted)]
   [InlineData(IsolationLevel.RepeatableRead)]
   public async Task Should_honor_default_isolation_level_if_no_level_is_provided(IsolationLevel childIsolationLevel)
   {
      await using var rootTx = await SUT.BeginTransactionAsync();

      await using var childTx = await SUT.BeginTransactionAsync(childIsolationLevel);

      childTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());
   }

   [Theory]
   [InlineData(IsolationLevel.ReadUncommitted, "READ-UNCOMMITTED")]
   [InlineData(IsolationLevel.ReadCommitted, "READ-COMMITTED")]
   [InlineData(IsolationLevel.RepeatableRead, "REPEATABLE-READ")]
   [InlineData(IsolationLevel.Serializable, "SERIALIZABLE")]
   public async Task Should_apply_isolation_level_to_root_transaction(IsolationLevel isolationLevel, string expectedServerIsolationLevel)
   {
      await using var rootTx = await SUT.BeginTransactionAsync(isolationLevel);

      (await GetServerIsolationLevelAsync()).Should().Be(expectedServerIsolationLevel);
   }

   [Fact]
   public async Task Should_use_repeatable_read_for_root_transaction_if_no_level_is_provided()
   {
      await using var rootTx = await SUT.BeginTransactionAsync();

      (await GetServerIsolationLevelAsync()).Should().Be("REPEATABLE-READ");
   }

   [Fact]
   public async Task Should_commit_bulk_insert_and_save_changes_of_nested_transactions()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         ActDbContext.TestEntities.Add(new TestEntity { Id = _FIRST_ID, RequiredName = "SaveChanges" });
         await ActDbContext.SaveChangesAsync();

         await using (var child = await SUT.BeginTransactionAsync())
         {
            await ActDbContext.BulkInsertAsync([new TestEntity { Id = _SECOND_ID, RequiredName = "BulkInsert" }]);
            await child.CommitAsync();
         }

         await tx.CommitAsync();
      }

      var names = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).Select(e => e.RequiredName).ToListAsync();
      names.Should().Equal("BulkInsert", "SaveChanges");
   }

   [Fact]
   public async Task Should_keep_bulk_insert_of_nested_transactions_invisible_until_the_root_transaction_is_committed()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         await using (var child = await SUT.BeginTransactionAsync())
         {
            await ActDbContext.BulkInsertAsync([new TestEntity { Id = _FIRST_ID, RequiredName = "BulkInsert" }]);
            await child.CommitAsync();
         }

         // Committing the child doesn't commit anything on the database.
         (await ActDbContext.TestEntities.CountAsync()).Should().Be(1);
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);

         await tx.CommitAsync();
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_roll_back_bulk_insert_if_a_child_transaction_rolls_back()
   {
      await using (var tx = await SUT.BeginTransactionAsync())
      {
         await using (var child = await SUT.BeginTransactionAsync())
         {
            await ActDbContext.BulkInsertAsync([new TestEntity { Id = _FIRST_ID, RequiredName = "BulkInsert" }]);
            await child.RollbackAsync();
         }

         await tx.Awaiting(rootTx => rootTx.CommitAsync())
                 .Should().ThrowAsync<TransactionAbortedException>().WithMessage("The transaction has aborted.");
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_roll_back_bulk_update_and_bulk_upsert_of_nested_transactions_with_the_root_transaction()
   {
      await ArrangeDbContext.BulkInsertAsync([new TestEntity { Id = _FIRST_ID, RequiredName = "Before", Count = 1 }]);

      await using (var tx = await SUT.BeginTransactionAsync())
      await using (var child = await SUT.BeginTransactionAsync())
      {
         await ActDbContext.BulkUpdateAsync([new TestEntity { Id = _FIRST_ID, RequiredName = "After", Count = 2 }]);
         await ActDbContext.BulkInsertOrUpdateAsync([new TestEntity { Id = _SECOND_ID, RequiredName = "Upserted", Count = 3 }]);

         (await ActDbContext.TestEntities.CountAsync()).Should().Be(2);

         await child.CommitAsync();
         await tx.RollbackAsync();
      }

      var entity = await AssertDbContext.TestEntities.SingleAsync();
      entity.Id.Should().Be(_FIRST_ID);
      entity.RequiredName.Should().Be("Before");
      entity.Count.Should().Be(1);
   }

   private Task<string> GetServerIsolationLevelAsync()
   {
      return ActDbContext.Database.SqlQueryRaw<string>("SELECT @@transaction_isolation AS `Value`").SingleAsync();
   }

   private async Task<bool> IsTransactionUsableAsync(DbTransaction tx)
   {
      try
      {
         var connection = ActDbContext.Database.GetDbConnection();
         await using var command = connection.CreateCommand();
         command.Transaction = tx;
         command.CommandText = "SELECT VERSION();";
         await command.ExecuteNonQueryAsync();
      }
      catch (InvalidOperationException)
      {
         // MySqlConnector: "The transaction associated with this command is not the connection's active transaction".
         return false;
      }

      return true;
   }
}
