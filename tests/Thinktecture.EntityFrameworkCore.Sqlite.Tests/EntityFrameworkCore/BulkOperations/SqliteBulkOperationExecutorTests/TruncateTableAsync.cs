using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class TruncateTableAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;
   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public TruncateTableAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_table_is_empty()
   {
      await SUT.Awaiting(sut => sut.TruncateTableAsync<TestEntity>())
               .Should().NotThrowAsync();
   }

   [Fact]
   public async Task Should_delete_entities()
   {
      ArrangeDbContext.Add(new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              RequiredName = "RequiredName",
                              Count = 42
                           });
      await ArrangeDbContext.SaveChangesAsync();

      await SUT.TruncateTableAsync<TestEntity>();

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_keep_braces_in_table_name()
   {
      // The table does not exist. The error of the database proves that the name arrives with its braces.
      ConfigureModel = builder => builder.Entity<BraceNamedEntity>().ToTable("missing{table}");
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      (await SUT.Awaiting(sut => sut.TruncateTableAsync<BraceNamedEntity>())
                .Should().ThrowAsync<SqliteException>())
         .Which.Message.Should().Contain("no such table: missing{table}");
   }

   private sealed class BraceNamedEntity
   {
      public int Id { get; set; }
   }
}
