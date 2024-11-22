using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests;

public class ResetState : NestedRelationalTransactionManagerTestBase
{
   public ResetState(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_do_nothing_if_not_transaction_is_active()
   {
      SUT.ResetState();

      SUT.CurrentTransaction.Should().BeNull();
   }

   [Fact]
   public void Should_dispose_open_root_transaction()
   {
      var rootTx = SUT.BeginTransaction();

      SUT.ResetState();

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
   }

   [Fact]
   public void Should_dispose_current_child_and_root_transactions()
   {
      // ReSharper disable UnusedVariable
      var rootTx = SUT.BeginTransaction();
      var childTx = SUT.BeginTransaction();
      var secondChildTx = SUT.BeginTransaction();
      // ReSharper restore UnusedVariable

      SUT.ResetState();

      SUT.CurrentTransaction.Should().BeNull();
      IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
   }
}