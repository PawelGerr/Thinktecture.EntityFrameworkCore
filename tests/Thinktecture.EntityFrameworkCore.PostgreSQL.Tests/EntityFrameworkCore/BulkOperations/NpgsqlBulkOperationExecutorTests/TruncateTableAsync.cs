using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class TruncateTableAsync : IntegrationTestsBase
{
   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public TruncateTableAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_table_is_empty()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();

      await SUT.Awaiting(sut => sut.TruncateTableAsync<TestEntity>())
               .Should().NotThrowAsync();
   }

   [Fact]
   public async Task Should_delete_entities()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();

      ArrangeDbContext.Add(new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           });
      await ArrangeDbContext.SaveChangesAsync();

      await SUT.TruncateTableAsync<TestEntity>();

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_delete_entities_with_cascade_false()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();

      ArrangeDbContext.Add(new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           });
      await ArrangeDbContext.SaveChangesAsync();

      await SUT.TruncateTableAsync<TestEntity>(cascade: false);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_delete_entities_with_cascade_true()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();

      ArrangeDbContext.Add(new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           });
      await ArrangeDbContext.SaveChangesAsync();

      await SUT.TruncateTableAsync<TestEntity>(cascade: true);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }
}
