using System;
using System.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedDbContextTransactionTests;

public class Commit : NestedRelationalTransactionManagerTestBase
{
   public Commit(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, MigrationExecutionStrategies.EnsureCreated)
   {
   }

   [Fact]
   public void Should_throw_when_trying_to_commit_twice()
   {
      var rootTx = SUT.BeginTransaction();
      ActDbContext.Add(new TestEntity());
      ActDbContext.SaveChanges();

      rootTx.Commit();

      rootTx.Invoking(tx => tx.Commit())
            .Should().Throw<InvalidOperationException>().WithMessage("This root transaction has completed; it is no longer usable.");

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_commit_root_transaction_and_underlying_transaction()
   {
      var rootTx = SUT.BeginTransaction();
      ActDbContext.Add(new TestEntity());
      ActDbContext.SaveChanges();

      rootTx.Commit();

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_commit_child_transaction_but_not_underlying_transaction()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();

      childTx.Commit();

      SUT.CurrentTransaction.Should().Be(rootTx);
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_commit_child_and_root_transaction()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      ActDbContext.Add(new TestEntity());
      ActDbContext.SaveChanges();

      childTx.Commit();
      rootTx.Commit();

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_commit_children_and_root_transactions()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      var anotherChildTx = SUT.BeginTransaction();
      ActDbContext.Add(new TestEntity());
      ActDbContext.SaveChanges();

      anotherChildTx.Commit();
      childTx.Commit();
      rootTx.Commit();

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
      AssertDbContext.TestEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_throw_when_committing_root_transaction_if_child_transaction_is_rolled_back()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();

      childTx.Rollback();

      rootTx.Invoking(tx => tx.Commit())
            .Should().Throw<TransactionAbortedException>().WithMessage("The transaction has aborted.");

      SUT.CurrentTransaction.Should().Be(rootTx);
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_throw_when_committing_root_transaction_if_child_transaction_is_rolled_back_due_to_disposed()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();

      childTx.Dispose();

      rootTx.Invoking(tx => tx.Commit())
            .Should().Throw<TransactionAbortedException>().WithMessage("The transaction has aborted.");

      SUT.CurrentTransaction.Should().Be(rootTx);
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_throw_when_committing_child_transaction_if_another_child_transaction_is_rolled_back()
   {
      // ReSharper disable once UnusedVariable
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      var anotherChildTx = SUT.BeginTransaction();

      anotherChildTx.Rollback();

      childTx.Invoking(tx => tx.Commit())
             .Should().Throw<TransactionAbortedException>().WithMessage("The transaction has aborted.");

      SUT.CurrentTransaction.Should().Be(childTx);
      IsTransactionUsable(childTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_throw_when_committing_child_transaction_if_another_child_transaction_is_rolled_back_due_to_disposed()
   {
      // ReSharper disable once UnusedVariable
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      var anotherChildTx = SUT.BeginTransaction();

      anotherChildTx.Dispose();

      childTx.Invoking(tx => tx.Commit())
             .Should().Throw<TransactionAbortedException>().WithMessage("The transaction has aborted.");

      SUT.CurrentTransaction.Should().Be(childTx);
      IsTransactionUsable(childTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_throw_when_committing_root_transaction_if_child_transaction_is_not_completed()
   {
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();

      rootTx.Invoking(tx => tx.Commit())
            .Should().Throw<InvalidOperationException>().WithMessage("Transactions nested incorrectly. At least one of the child transactions is not completed.");

      SUT.CurrentTransaction.Should().Be(childTx);
      IsTransactionUsable(childTx.GetDbTransaction()).Should().BeTrue();
   }

   [Fact]
   public void Should_throw_when_committing_child_transaction_if_another_child_transaction_is_not_completed()
   {
      // ReSharper disable once UnusedVariable
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      var anotherChildTx = SUT.BeginTransaction();

      childTx.Invoking(tx => tx.Commit())
             .Should().Throw<InvalidOperationException>().WithMessage("Transactions nested incorrectly. At least one of the child transactions is not completed.");

      SUT.CurrentTransaction.Should().Be(anotherChildTx);
      IsTransactionUsable(anotherChildTx.GetDbTransaction()).Should().BeTrue();
   }
}