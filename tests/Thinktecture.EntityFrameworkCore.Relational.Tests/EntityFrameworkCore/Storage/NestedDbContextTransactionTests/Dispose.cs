using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Storage.NestedRelationalTransactionManagerTests;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Storage.NestedDbContextTransactionTests
{
   public class Dispose : NestedRelationalTransactionManagerTestBase
   {
      public Dispose(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, MigrationExecutionStrategies.EnsureCreated)
      {
      }

      [Fact]
      public void Should_rollback_uncompleted_root_transaction()
      {
         var rootTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         rootTx.Dispose();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_do_nothing_when_disposing_multiple_times()
      {
         var rootTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         rootTx.Dispose();
         rootTx.Dispose();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }

      [Fact]
      public void Should_rollback_uncompleted_child_transaction()
      {
         var rootTx = SUT.BeginTransaction();
         var childTx = SUT.BeginTransaction();
         ActDbContext.Add(new TestEntity());
         ActDbContext.SaveChanges();

         childTx.Dispose();

         SUT.CurrentTransaction.Should().Be(rootTx);
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeTrue();
         AssertDbContext.TestEntities.Should().HaveCount(1);

         rootTx.Dispose();

         SUT.CurrentTransaction.Should().BeNull();
         IsTransactionUsable(rootTx.GetDbTransaction()).Should().BeFalse();
         AssertDbContext.TestEntities.Should().HaveCount(0);
      }
   }
}
