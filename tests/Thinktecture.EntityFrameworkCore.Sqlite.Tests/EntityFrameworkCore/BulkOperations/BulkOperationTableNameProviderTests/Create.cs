using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameProviderTests;

// ReSharper disable once InconsistentNaming
public class Create : SchemaChangingIntegrationTestsBase
{
   public Create(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_throw_when_delegate_is_null()
   {
      FluentActions.Invoking(() => IBulkOperationTableNameProvider.Create(null!))
                   .Should().Throw<ArgumentNullException>();
   }

   [Fact]
   public void Should_return_table_name_from_delegate()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      IEntityType? receivedEntityType = null;

      var provider = IBulkOperationTableNameProvider.Create(t =>
                                                            {
                                                               receivedEntityType = t;
                                                               return new BulkOperationTableName("X", "s");
                                                            });

      provider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", "s"));
      receivedEntityType.Should().BeSameAs(entityType);
   }
}
