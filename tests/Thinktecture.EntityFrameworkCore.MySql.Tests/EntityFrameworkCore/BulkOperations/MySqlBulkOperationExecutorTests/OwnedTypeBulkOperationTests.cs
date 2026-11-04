namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class OwnedTypeBulkOperationTests : MySqlIntegrationTestsBase
{
   public OwnedTypeBulkOperationTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_reject_bulk_updates_of_separately_mapped_owned_types()
   {
      var act = () => ActDbContext.BulkUpdateAsync(
                                                   [new MySqlOwnerEntity { Id = 1, Address = new MySqlAddress { City = "Berlin" } }]);

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*owned entities*");
   }

   [Fact]
   public async Task Should_reject_upserts_of_separately_mapped_owned_types()
   {
      var act = () => ActDbContext.BulkInsertOrUpdateAsync(
                                                           [new MySqlOwnerEntity { Id = 1, Address = new MySqlAddress { City = "Berlin" } }]);

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*owned entities*");
   }
}
