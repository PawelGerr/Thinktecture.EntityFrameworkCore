using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedDbContextTransactionTests
{
   public class Rollback : NestedRelationalTransactionManagerTestBase
   {
      public Rollback(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, MigrationExecutionStrategies.EnsureCreated)
      {
      }

      [Fact]
      public void Should_throw_when_trying_to_rollback_twice()
      {
         var rootTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         rootTx.Rollback();

         rootTx.Invoking(tx => tx.Rollback())
               .Should().Throw<InvalidOperationException>().WithMessage("This root transaction has completed; it is no longer usable.");

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_root_transaction_and_underlying_transaction()
      {
         var rootTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         rootTx.Rollback();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().BeEmpty();
      }

      [Fact]
      public void Should_rollback_child_transaction_but_not_underlying_transaction()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();

         childTx.Rollback();

         SUT.CurrentTransaction.Should().Be(rootTx);
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
      }

      [Fact]
      public void Should_rollback_child_and_root_transaction()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Rollback();
         rootTx.Rollback();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_children_and_root_transactions()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         var anotherChildTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         anotherChildTx.Rollback();
         childTx.Rollback();
         rootTx.Rollback();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_root_transaction_if_child_transaction_is_committed()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Commit();
         rootTx.Rollback();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_root_transaction_if_child_transaction_is_rolled_backed_due_to_dispose()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Dispose();
         rootTx.Rollback();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_child_transaction_if_another_child_transaction_is_committed()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         var anotherChildTx = SUT.BeginTransaction();

         anotherChildTx.Commit();
         childTx.Rollback();

         SUT.CurrentTransaction.Should().Be(rootTx);
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
      }

      [Fact]
      public void Should_rollback_child_transaction_if_another_child_transaction_is_rolled_back_due_to_disposed()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         var anotherChildTx = SUT.BeginTransaction();

         anotherChildTx.Dispose();
         childTx.Rollback();

         SUT.CurrentTransaction.Should().Be(rootTx);
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
      }

      [Fact]
      public void Should_throw_when_rollback_root_transaction_if_child_transaction_is_not_completed()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();

         rootTx.Invoking(tx => tx.Rollback())
               .Should().Throw<InvalidOperationException>().WithMessage("Transactions nested incorrectly. At least one of the child transactions is not completed.");

         SUT.CurrentTransaction.Should().Be(childTx);
         IsTransactionUsable(childTx.GetDbTransaction()).Should().BeTrue();
      }

      [Fact]
      public void Should_throw_when_rollback_child_transaction_if_another_child_transaction_is_not_completed()
      {
         // ReSharper disable once UnusedVariable
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         var anotherChildTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Invoking(tx => tx.Rollback())
                .Should().Throw<InvalidOperationException>().WithMessage("Transactions nested incorrectly. At least one of the child transactions is not completed.");

         SUT.CurrentTransaction.Should().Be(anotherChildTx);
         IsTransactionUsable(anotherChildTx.GetDbTransaction()).Should().BeTrue();
      }
   }
}
