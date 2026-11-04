using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkCopyTests : MySqlIntegrationTestsBase
{
   public MySqlBulkCopyTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_null_values()
   {
      await using var context = await CreateBulkCopyContextAsync();

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
         [new MySqlTestEntity { Id = 1, Name = null }],
         new MySqlBulkInsertOptions { UseBulkCopy = true });

      affectedRows.Should().Be(1);
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().BeNull();
   }

   [Fact]
   public async Task Should_fail_instead_of_silently_dropping_rows_with_duplicate_keys()
   {
      await using var context = await CreateBulkCopyContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Existing" }]);

      var act = () => context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
         [new MySqlTestEntity { Id = 2, Name = "New" }, new MySqlTestEntity { Id = 3, Name = "Existing" }],
         new MySqlBulkInsertOptions { UseBulkCopy = true });

      // MySqlConnector compares the copied and inserted row counts itself; the warnings check covers the remaining silent cases.
      await act.Should().ThrowAsync<MySqlConnector.MySqlException>().WithMessage("*only 1 was inserted*");
      (await context.Entities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_insert_typed_values()
   {
      await using var context = await CreateBulkCopyContextAsync();
      var id = Guid.NewGuid();

      await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
         [new MySqlTypedEntity { Id = id, Amount = 12.5m, Status = MySqlTestStatus.Published, CreatedAt = new DateTime(2020, 1, 2, 3, 4, 5), Data = [1, 2], Flag = true }],
         new MySqlBulkInsertOptions { UseBulkCopy = true });

      var loaded = await context.TypedEntities.AsNoTracking().SingleAsync();
      loaded.Id.Should().Be(id);
      loaded.Status.Should().Be(MySqlTestStatus.Published);
      loaded.Data.Should().Equal(1, 2);
   }

   private async Task<MySqlTestDbContext> CreateBulkCopyContextAsync()
   {
      var context = await CreateContextAsync();
      var builder = new MySqlConnector.MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = true };
      context.Database.SetConnectionString(builder.ConnectionString);
      return context;
   }
}
