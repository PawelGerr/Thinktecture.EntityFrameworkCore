using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryWithBinaryColumnAsync : SchemaChangingIntegrationTestsBase
{
   public BulkUpdateFromQueryWithBinaryColumnAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, IMigrationExecutionStrategy.EnsureCreated)
   {
   }

   protected override void ConfigureTestDbContextProvider(SqliteTestDbContextProviderBuilder<TestDbContext> builder)
   {
      base.ConfigureTestDbContextProvider(builder);
      builder.CollectExecutedCommands(true);
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_SequenceEqual_of_binary_column_and_captured_array()
   {
      ConfigureModel = builder => builder.Entity<EntityWithBinary>();

      var match = new EntityWithBinary { Id = new Guid("8D000000-0000-0000-0000-00000000000B"), Bytes = [1, 2] };
      var other = new EntityWithBinary { Id = new Guid("8D000000-0000-0000-0000-00000000000C"), Bytes = [3, 4] };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      byte[] captured = [1, 2];

      var affectedRows = await ActDbContext.Set<EntityWithBinary>().BulkUpdateAsync(
         ActDbContext.Set<EntityWithBinary>(),
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => s.Bytes.SequenceEqual(captured));

      affectedRows.Should().Be(1);
      TestCtxProvider.ExecutedCommands!.Last().Should().Contain("\"Bytes\" = X'0102'");

      (await AssertDbContext.Set<EntityWithBinary>().SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.Set<EntityWithBinary>().SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   public class EntityWithBinary
   {
      public Guid Id { get; set; }
      public byte[] Bytes { get; set; } = [];
      public int Count { get; set; }
   }
}
