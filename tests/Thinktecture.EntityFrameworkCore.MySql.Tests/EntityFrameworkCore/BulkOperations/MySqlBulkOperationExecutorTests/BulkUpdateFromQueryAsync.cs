using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class BulkUpdateFromQueryAsync : MySqlIntegrationTestsBase
{
   private const string _TARGET_TABLE = "query_targets_override";

   public BulkUpdateFromQueryAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_update_the_table_selected_by_the_table_name_provider()
   {
      await ArrangeDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE `{_TARGET_TABLE}` LIKE `QueryTargets`");
      await ArrangeDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO `{_TARGET_TABLE}` (`Id`, `Name`, `Count`) VALUES (1, 'before', 1)");
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "after", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                                                                         ActDbContext.QuerySources.AsNoTracking(),
                                                                         target => target.Id,
                                                                         source => source.Id,
                                                                         setters => setters
                                                                                    .Set(target => target.Name, (_, source) => source.Name)
                                                                                    .Set(target => target.Count, (_, source) => source.Count),
                                                                         options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET_TABLE) });

      affectedRows.Should().Be(1);
      (await AssertDbContext.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{_TARGET_TABLE}`").SingleAsync()).Should().Be("after");
      ExecutedCommands.Should().Contain(command => command.Contains($"UPDATE `{_TARGET_TABLE}`", StringComparison.Ordinal));
   }
}
