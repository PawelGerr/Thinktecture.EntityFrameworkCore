using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTableNameProviderTests : MySqlIntegrationTestsBase
{
   private const string _TARGET = "tn_target";

   public MySqlTableNameProviderTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_into_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();

      await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
         [new MySqlTestEntity { Id = 1, Name = "a" }],
         new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET) });

      (await CountAsync(context, _TARGET)).Should().Be(1);
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_upsert_into_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();
      var options = new MySqlBulkInsertOrUpdateOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET) };
      var executor = context.GetService<MySqlBulkOperationExecutor>();

      await executor.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "a" }], options);
      await executor.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "b" }], options);

      (await CountAsync(context, _TARGET)).Should().Be(1);
      (await context.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{_TARGET}`").SingleAsync()).Should().Be("b");
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_update_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();
      await context.Database.ExecuteSqlRawAsync($"INSERT INTO `{_TARGET}` (`Id`, `Name`) VALUES (1, 'before')");

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlTestEntity { Id = 1, Name = "after" }],
         new MySqlBulkUpdateOptions
         {
            PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTestEntity>(entity => entity.Name),
            TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET)
         });

      affectedRows.Should().Be(1);
      (await context.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{_TARGET}`").SingleAsync()).Should().Be("after");
   }

   private async Task<MySqlTestDbContext> CreateContextWithTargetTableAsync()
   {
      var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TARGET}`");
      await context.Database.ExecuteSqlRawAsync($"CREATE TABLE `{_TARGET}` LIKE `Entities`");
      return context;
   }

   private static async Task<int> CountAsync(MySqlTestDbContext context, string table)
   {
      return await context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS `Value` FROM `{table}`").SingleAsync();
   }
}
