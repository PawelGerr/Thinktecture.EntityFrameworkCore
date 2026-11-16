using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbContextOptionsBuilderExtensionsTests;

// MySQL specifics:
// - The contexts of the base class register bulk operations and window functions, which enable the factory as well.
//   Therefore the act context is built with the factory as the only Thinktecture feature.
// ReSharper disable InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class AddCustomQueryableMethodTranslatingExpressionVisitorFactory : MySqlIntegrationTestsBase
{
   public AddCustomQueryableMethodTranslatingExpressionVisitorFactory(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_enable_AsSubQuery_translation()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var options = MySqlTestDbContext.CreateOptionsBuilder()
                                      .UseMySql(ConnectionString, MySqlContainerFixture.ServerVersion, mySqlOptions => mySqlOptions.AddCustomQueryableMethodTranslatingExpressionVisitorFactory())
                                      .Options;
      await using var actDbContext = new MySqlTestDbContext(options);

      var result = await actDbContext.TestEntities
                                     .Select(e => new { e.Id, e.Name })
                                     .AsSubQuery()
                                     .Where(e => e.Name == "1")
                                     .ToListAsync();

      result.Should().HaveCount(1);
      result[0].Name.Should().Be("1");
   }
}
