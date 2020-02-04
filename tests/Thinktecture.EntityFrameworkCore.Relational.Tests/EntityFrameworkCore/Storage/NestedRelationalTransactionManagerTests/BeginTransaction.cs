using System;
using System.Collections.Generic;
using System.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests
{
   public class BeginTransaction : NestedRelationalTransactionManagerTestBase
   {
      public BeginTransaction(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_create_new_root_transaction()
      {
         SUT.CurrentTransaction.Should().BeNull();

         var tx = SUT.BeginTransaction();

         tx.Should().NotBeNull();
         tx.Should().Be(SUT.CurrentTransaction);
         tx.GetDbTransaction().Should().NotBeNull();
      }

      [Fact]
      public void Should_create_new_child_transaction_if_root_transaction_exist()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();

         childTx.Should().NotBeNull();
         childTx.Should().Be(SUT.CurrentTransaction);
         childTx.GetDbTransaction().Should().NotBeNull();
         childTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());
      }

      [Fact]
      public void Should_create_new_child_transaction_if_another_child_transaction_exist()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         var secondChildTx = SUT.BeginTransaction();

         secondChildTx.Should().NotBeNull();
         secondChildTx.Should().Be(SUT.CurrentTransaction);
         secondChildTx.GetDbTransaction().Should().NotBeNull();
         secondChildTx.GetDbTransaction().Should().Be(childTx.GetDbTransaction());
         secondChildTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());
      }

      public static readonly IEnumerable<object[]> TransactionCompletions = new List<object[]>
                                                                            {
                                                                               new object[] { new Action<IDbContextTransaction>(tx => tx.Commit()) },
                                                                               new object[] { new Action<IDbContextTransaction>(tx => tx.Rollback()) },
                                                                               new object[] { new Action<IDbContextTransaction>(tx => tx.Dispose()) }
                                                                            };

      [Theory]
      [MemberData(nameof(TransactionCompletions))]
      public void Should_create_new_child_transaction_from_root_after_another_child_is_completed(Action<IDbContextTransaction> action)
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();

         action(childTx);

         var secondChildTx = SUT.BeginTransaction();

         secondChildTx.Should().NotBeNull();
         secondChildTx.Should().Be(SUT.CurrentTransaction);
         secondChildTx.GetDbTransaction().Should().NotBeNull();
         secondChildTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());

         SUT.RollbackTransaction(); // rollback of secondChildTx
         SUT.RollbackTransaction(); // rollback of rootTx

         SUT.CurrentTransaction.Should().BeNull();
      }

      [Fact]
      public void Should_allow_create_child_transactions_with_lower_level()
      {
         // ReSharper disable UnusedVariable
         var rootTx = SUT.BeginTransaction(IsolationLevel.Serializable);
         var childTx = SUT.BeginTransaction(IsolationLevel.RepeatableRead);
         // ReSharper restore UnusedVariable
      }

      [Fact]
      public void Should_allow_create_child_transactions_with_higher_level_if_underlying_transaction_uses_higher_level_internally()
      {
         var rootTx = SUT.BeginTransaction(IsolationLevel.RepeatableRead);
         rootTx.GetDbTransaction().IsolationLevel.Should().Be(IsolationLevel.Serializable); // sqlite uses "Serializable" internally

         // ReSharper disable once UnusedVariable
         var childTx = SUT.BeginTransaction(IsolationLevel.Serializable);
      }

      [Fact]
      public void Should_allow_create_child_transactions_with_no_isolation_level()
      {
         // ReSharper disable UnusedVariable
         var rootTx = SUT.BeginTransaction(IsolationLevel.Serializable);
         var childTx = SUT.BeginTransaction();
         // ReSharper restore UnusedVariable
      }

      [Fact]
      public void Should_not_allow_create_child_transactions_having_Snapshot()
      {
         SUT.BeginTransaction(IsolationLevel.Serializable);

         SUT.Invoking(sut => sut.BeginTransaction(IsolationLevel.Snapshot))
            .Should().Throw<InvalidOperationException>().WithMessage("The isolation level 'Serializable' of the parent transaction is not compatible to the provided isolation level 'Snapshot'.");
      }

      [Fact]
      public void Should_not_allow_create_child_transactions_having_Chaos()
      {
         SUT.BeginTransaction(IsolationLevel.Serializable);

         SUT.Invoking(sut => sut.BeginTransaction(IsolationLevel.Chaos))
            .Should().Throw<InvalidOperationException>().WithMessage("The isolation level 'Serializable' of the parent transaction is not compatible to the provided isolation level 'Chaos'.");
      }

      [Fact]
      public void Should_not_allow_create_child_transactions_having_Unspecified()
      {
         SUT.BeginTransaction(IsolationLevel.Serializable);

         SUT.Invoking(sut => sut.BeginTransaction(IsolationLevel.Unspecified))
            .Should().Throw<ArgumentException>().WithMessage("The isolation level 'Unspecified' is not allowed. (Parameter 'isolationLevel')");
      }
   }
}
