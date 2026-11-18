using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class TruncateTableAsync : IntegrationTestsBase
{
   private SqlServerBulkOperationExecutor? _sut;
   private SqlServerBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqlServerBulkOperationExecutor>();

   public TruncateTableAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
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
   public async Task Should_keep_braces_in_table_name()
   {
      // CustomTempTable is excluded from the data cleanup of the test base (NonExistingTableFilter), so the missing table does not break the cleanup.
      ConfigureModel = builder => builder.Entity<CustomTempTable>().HasNoKey().ToTable("missing{table}");

      (await SUT.Awaiting(sut => sut.TruncateTableAsync<CustomTempTable>())
                .Should().ThrowAsync<SqlException>())
         .Which.Message.Should().Contain("missing{table}");
   }
}
