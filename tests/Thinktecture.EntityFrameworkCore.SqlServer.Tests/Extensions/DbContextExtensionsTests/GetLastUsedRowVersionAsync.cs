namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class GetLastUsedRowVersionAsync : IntegrationTestsBase
{
   public GetLastUsedRowVersionAsync(ITestOutputHelper testOutputHelper, SqlServerContainerFixture sqlServerContainerFixture)
      : base(testOutputHelper, sqlServerContainerFixture)
   {
   }

   [Fact]
   public async Task Should_fetch_last_used_rowversion()
   {
      var rowVersion = await ActDbContext.GetLastUsedRowVersionAsync(CancellationToken.None);
      rowVersion.Should().NotBe(0);
   }
}
