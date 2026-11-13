using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTempTableBulkInsertTests : MySqlIntegrationTestsBase
{
   public MySqlTempTableBulkInsertTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_entities_and_drop_table_on_dispose()
   {
      await using var context = await CreateContextAsync();
      string tableName;

      // Keep the connection open: a closed connection is reset by the pool, which drops temp tables on its own.
      await context.Database.OpenConnectionAsync();

      try
      {
         await using (var tempTable = await context.BulkInsertIntoTempTableAsync(
                         [new MySqlTestTempEntity { Id = 501, Name = "Temporary" }],
                         context.GetService<ITempTableBulkInsertExecutor>().CreateOptions()))
         {
            tableName = tempTable.Name;
            tempTable.NumberOfInsertedRows.Should().Be(1);
            (await tempTable.Query.Select(entity => entity.Name).SingleAsync()).Should().Be("Temporary");
         }

         var act = async () => await context.Database.ExecuteSqlRawAsync($"SELECT 1 FROM `{tableName}` LIMIT 1");

         // 1146: table doesn't exist
         (await act.Should().ThrowAsync<Exception>()).Which.GetBaseException().Should().BeOfType<MySqlConnector.MySqlException>()
                                                    .Which.Number.Should().Be(1146);
      }
      finally
      {
         await context.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_fail_with_error_1137_when_temp_table_is_referenced_twice_in_one_statement()
   {
      await using var context = await CreateContextAsync();
      await using var tempTable = await context.BulkInsertIntoTempTableAsync(
         [new MySqlTestTempEntity { Id = 601, Name = "Twice" }],
         context.GetService<ITempTableBulkInsertExecutor>().CreateOptions());

      var act = async () => await tempTable.Query.Join(tempTable.Query, left => left.Id, right => right.Id, (left, right) => left.Id).ToListAsync();

      (await act.Should().ThrowAsync<MySqlConnector.MySqlException>()).Which.Number.Should().Be(1137);
   }

   [Fact]
   public async Task Should_insert_primitive_values_into_temp_table()
   {
      await using var context = await CreateContextAsync();
      var executor = context.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertValuesIntoTempTableAsync(
         new[] { 11, 22, 33 },
         executor.CreateOptions(),
         CancellationToken.None);

      tempTable.NumberOfInsertedRows.Should().Be(3);
      (await tempTable.Query.OrderBy(value => value).ToArrayAsync())
         .Should().Equal(11, 22, 33);
   }

   [Fact]
   public async Task Should_append_entities_to_an_existing_temp_table()
   {
      await using var context = await CreateContextAsync();
      var tempTableReference = await context.CreateTempTableAsync<MySqlTestTempEntity>(new TempTableCreationOptions());

      try
      {
         await context.BulkInsertIntoTempTableAsync(
            [new MySqlTestTempEntity { Id = 511, Name = "Appended" }],
            tempTableReference,
            entity => new { entity.Id, entity.Name });

         var entityName = EntityNameProvider.GetTempTableName(typeof(MySqlTestTempEntity));
         var query = context.Set<MySqlTestTempEntity>(entityName).FromTempTable(new TempTableInfo(tempTableReference.Name));
         (await query.Select(entity => entity.Name).SingleAsync()).Should().Be("Appended");
      }
      finally
      {
         await tempTableReference.DisposeAsync();
      }
   }

   [Fact]
   public async Task Should_keep_session_temp_table_when_drop_on_dispose_is_disabled()
   {
      await using var context = await CreateContextAsync();
      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = new FixedTempTableNameProvider("keep_temp_table"),
                       DropTableOnDispose = false
                    };
      var tempTable = await context.CreateTempTableAsync<MySqlTestTempEntity>(options);

      try
      {
         await tempTable.DisposeAsync();

         context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Open);
         var rowCount = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM `keep_temp_table`").SingleAsync();
         rowCount.Should().Be(0);
      }
      finally
      {
         await context.Database.ExecuteSqlRawAsync("DROP TEMPORARY TABLE IF EXISTS `keep_temp_table`");
         await context.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_drop_and_recreate_existing_temp_table_when_requested()
   {
      await using var context = await CreateContextAsync();
      var provider = new FixedTempTableNameProvider("reuse_temp_table");
      var keepOptions = new TempTableCreationOptions { TableNameProvider = provider, DropTableOnDispose = false };
      var firstReference = await context.CreateTempTableAsync<MySqlTestTempEntity>(keepOptions);

      try
      {
         await context.BulkInsertIntoTempTableAsync(
            [new MySqlTestTempEntity { Id = 521, Name = "Old" }],
            firstReference,
            entity => new { entity.Id, entity.Name });
         await firstReference.DisposeAsync();

         var recreateOptions = new TempTableCreationOptions
                              {
                                 TableNameProvider = provider,
                                 TruncateTableIfExists = true,
                                 DropTableOnDispose = false
                              };
         var secondReference = await context.CreateTempTableAsync<MySqlTestTempEntity>(recreateOptions);

         try
         {
            var tableEntityName = EntityNameProvider.GetTempTableName(typeof(MySqlTestTempEntity));
            var query = context.Set<MySqlTestTempEntity>(tableEntityName).FromTempTable(new TempTableInfo(secondReference.Name));
            (await query.CountAsync()).Should().Be(0);

            await context.BulkInsertIntoTempTableAsync(
               [new MySqlTestTempEntity { Id = 522, Name = "New" }],
               secondReference,
               entity => new { entity.Id, entity.Name });

            (await query.Select(entity => entity.Name).SingleAsync()).Should().Be("New");
         }
         finally
         {
            await secondReference.DisposeAsync();
         }
      }
      finally
      {
         await context.Database.ExecuteSqlRawAsync("DROP TEMPORARY TABLE IF EXISTS `reuse_temp_table`");
         await context.Database.CloseConnectionAsync();
      }
   }

   private sealed class FixedTempTableNameProvider(string name) : ITempTableNameProvider
   {
      public ITempTableNameLease LeaseName(DbContext context, IEntityType entityType)
      {
         return new FixedTempTableNameLease(name);
      }
   }

   private sealed class FixedTempTableNameLease(string name) : ITempTableNameLease
   {
      public string Name { get; } = name;

      public void Dispose()
      {
      }
   }
}
