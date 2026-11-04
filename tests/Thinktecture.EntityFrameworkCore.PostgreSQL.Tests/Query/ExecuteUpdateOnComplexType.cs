using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Query;

// ReSharper disable once InconsistentNaming
public class ExecuteUpdateOnComplexType : IntegrationTestsBase
{
   public ExecuteUpdateOnComplexType(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_execute_update_on_struct_ComplexType_with_library_features_enabled()
   {
      var id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7");
      ArrangeDbContext.TestEntitiesWithStructComplexType.Add(new TestEntityWithStructComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = new TimestampPair(1, 2) });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType
                                           .ExecuteUpdateAsync(s => s.SetProperty(e => e.EffectiveMessage, new TimestampPair(7, 8)));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntitiesWithStructComplexType.SingleAsync();
      loaded.EffectiveMessage.Should().Be(new TimestampPair(7, 8));
   }
}
