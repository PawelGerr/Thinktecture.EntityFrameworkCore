namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class GetMinActiveRowVersionAsync : IntegrationTestsBase
{
   public GetMinActiveRowVersionAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public async Task Should_fetch_min_action_rowversion()
   {
      var rowVersion = await ActDbContext.GetMinActiveRowVersionAsync(CancellationToken.None);
      rowVersion.Should().NotBe(0);
   }
}
