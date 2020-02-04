using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests
{
   // ReSharper disable once InconsistentNaming
   public class BeginTransactionAsync : NestedRelationalTransactionManagerTestBase
   {
      public BeginTransactionAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public async Task Should_create_new_root_transaction()
      {
         SUT.CurrentTransaction.Should().BeNull();

         var tx = await SUT.BeginTransactionAsync();

         tx.Should().NotBeNull();
         tx.Should().Be(SUT.CurrentTransaction);
         tx.GetDbTransaction().Should().NotBeNull();
      }

      [Fact]
      public async Task Should_create_new_child_transaction_if_root_transaction_exist()
      {
         var rootTx = await SUT.BeginTransactionAsync();
         var childTx = await SUT.BeginTransactionAsync();

         childTx.Should().NotBeNull();
         childTx.Should().Be(SUT.CurrentTransaction);
         childTx.GetDbTransaction().Should().NotBeNull();
         childTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());
      }

      [Fact]
      public async Task Should_create_new_child_transaction_if_another_child_transaction_exist()
      {
         var rootTx = await SUT.BeginTransactionAsync();
         var childTx = await SUT.BeginTransactionAsync();
         var secondChildTx = await SUT.BeginTransactionAsync();

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
      public async Task Should_create_new_child_transaction_from_root_after_another_child_is_completed(Action<IDbContextTransaction> action)
      {
         var rootTx = await SUT.BeginTransactionAsync();
         var childTx = await SUT.BeginTransactionAsync();

         action(childTx);

         var secondChildTx = await SUT.BeginTransactionAsync();

         secondChildTx.Should().NotBeNull();
         secondChildTx.Should().Be(SUT.CurrentTransaction);
         secondChildTx.GetDbTransaction().Should().NotBeNull();
         secondChildTx.GetDbTransaction().Should().Be(rootTx.GetDbTransaction());

         SUT.RollbackTransaction(); // rollback of secondChildTx
         SUT.RollbackTransaction(); // rollback of rootTx

         SUT.CurrentTransaction.Should().BeNull();
      }
   }
}
