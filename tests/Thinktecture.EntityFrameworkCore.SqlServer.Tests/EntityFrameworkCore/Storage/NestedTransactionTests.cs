using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.TestDatabaseContext;
using IsolationLevel = System.Data.IsolationLevel;

namespace Thinktecture.EntityFrameworkCore.Storage;

public class NestedTransactionTests : IntegrationTestsBase
{
   protected NestedRelationalTransactionManager SUT => (NestedRelationalTransactionManager)ActDbContext.GetService<IDbContextTransactionManager>();

   public NestedTransactionTests(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, false)
   {
   }

   [Fact]
   public void Should_create_new_root_transaction()
   {
      SUT.CurrentTransaction.Should().BeNull();

      using (var tx = SUT.BeginTransaction())
      {
         tx.Should().NotBeNull();
         tx.Should().Be(SUT.CurrentTransaction);
         tx.GetDbTransaction().Should().NotBeNull();
         IsTransactionUsable(tx.GetDbTransaction()).Should().BeTrue();
      }
   }

   [Fact]
   public void Should_create_new_root_transaction_and_commit_correctly()
   {
      using (var tx = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         tx.Commit();
      }

      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_create_new_root_transaction_and_rollback_correctly()
   {
      using (var tx = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         tx.Rollback();
      }

      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_create_new_root_transaction_and_dispose_correctly()
   {
      // ReSharper disable once UnusedVariable
      using (var tx = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();
      }

      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_create_new_child_transaction()
   {
      using (var tx = SUT.BeginTransaction())
      using (var childTx = SUT.BeginTransaction())
      {
         childTx.Should().NotBeNull();
         childTx.Should().Be(SUT.CurrentTransaction);
         childTx.GetDbTransaction().Should().NotBeNull();
         childTx.GetDbTransaction().Should().Be(tx.GetDbTransaction());
         IsTransactionUsable(childTx.GetDbTransaction()).Should().BeTrue();
      }
   }

   [Fact]
   public void Should_create_new_child_transaction_and_commit_correctly()
   {
      using (var tx = SUT.BeginTransaction())
      using (var child = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         child.Commit();
         tx.Commit();
      }

      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_create_new_child_transaction_and_rollback_correctly()
   {
      using (var tx = SUT.BeginTransaction())
      using (var childTx = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Rollback();
         tx.Rollback();
      }

      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_create_new_child_transaction_and_dispose_correctly()
   {
      using (var tx = SUT.BeginTransaction())
      using (var childTx = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();
      }

      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_complete_underlying_translations_after_root_is_completed()
   {
      using (var tx = SUT.BeginTransaction())
      using (var child = SUT.BeginTransaction())
      {
         ActDbContext.TestEntities.Add(new TestEntity { Id = new Guid("7631FF3B-E5B1-485A-ABD5-386C6B37E991") });
         ActDbContext.SaveChanges();

         child.Commit();
         ActDbContext.TestEntities.Add(new TestEntity { Id = new Guid("7A4397B0-C9B6-46FF-B8C8-F39A9D6F5105") });
         ActDbContext.SaveChanges();

         tx.Rollback();

         // outside of the transaction
         ActDbContext.TestEntities.Add(new TestEntity { Id = new Guid("CB682609-1503-4FBE-8A29-66E2DF8F00E5") });
         ActDbContext.SaveChanges();
      }

      AssertDbContext.TestEntities.Should().HaveCount(1);
      AssertDbContext.TestEntities.First().Id.Should().Be(new Guid("CB682609-1503-4FBE-8A29-66E2DF8F00E5"));
   }

   [Fact]
   public void Should_rollback_if_a_sibling_transaction_rollbacks()
   {
      using (var tx = SUT.BeginTransaction())
      {
         using (var child = SUT.BeginTransaction())
         {
            child.Rollback();
         }

         using (var child = SUT.BeginTransaction())
         {
            child.Commit();
         }

         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         tx.Invoking(rootTx => rootTx.Commit())
           .Should().Throw<TransactionAbortedException>().WithMessage("The transaction has aborted.");
      }

      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_dispose_child_transactions_if_root_transactions_is_disposed()
   {
      using (var tx = SUT.BeginTransaction())
      {
         var child = SUT.BeginTransaction();

         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();
      }

      SUT.CurrentTransaction.Should().BeNull();
      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_dispose_inner_child_transactions_if_outer_transactions_is_disposed()
   {
      using (var tx = SUT.BeginTransaction())
      {
         var child = SUT.BeginTransaction();
         var otherChild = SUT.BeginTransaction();

         ActDbContext.TestEntities.Add(new TestEntity());
         ActDbContext.SaveChanges();

         child.Dispose();
      }

      SUT.CurrentTransaction.Should().BeNull();
      AssertDbContext.TestEntities.Should().HaveCount(0);
   }

   [Fact]
   public void Should_not_allow_create_child_transactions_with_higher_level()
   {
      using (var rootTx = SUT.BeginTransaction(IsolationLevel.RepeatableRead))
      {
         rootTx.GetDbTransaction().IsolationLevel.Should().Be(IsolationLevel.RepeatableRead);

         SUT.Invoking(sut => sut.BeginTransaction(IsolationLevel.Serializable))
            .Should().Throw<InvalidOperationException>().WithMessage("The isolation level 'RepeatableRead' of the parent transaction is not compatible to the provided isolation level 'Serializable'.");
      }
   }

   [Fact]
   public void Should_honor_default_isolation_level_if_no_level_is_provided()
   {
      using (var rootTx = SUT.BeginTransaction())
      {
         rootTx.GetDbTransaction().IsolationLevel.Should().Be(IsolationLevel.ReadCommitted);

         var childTx = SUT.BeginTransaction(IsolationLevel.ReadCommitted);
      }
   }

   private bool IsTransactionUsable(DbTransaction tx)
   {
      try
      {
         var connection = ActDbContext.Database.GetDbConnection();
         var command = connection.CreateCommand();
         command.Transaction = tx;
         command.CommandText = "SELECT @@version;";
         command.ExecuteNonQuery();
      }
      catch (InvalidOperationException ex)
      {
         if (ex.Message == "The transaction object is not associated with the connection object.")
            return false;
      }

      return true;
   }
}
