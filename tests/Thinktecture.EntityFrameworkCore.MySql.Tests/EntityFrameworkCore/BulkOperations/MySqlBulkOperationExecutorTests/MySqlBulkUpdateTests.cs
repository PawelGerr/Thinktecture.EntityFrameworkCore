using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkUpdateTests : MySqlIntegrationTestsBase
{
   public MySqlBulkUpdateTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_update_only_selected_property()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 201, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.BulkUpdateAsync(
         [new MySqlTestEntity { Id = 201, Name = "After" }],
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      affectedRows.Should().Be(1);
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("After");
   }

   [Fact]
   public async Task Should_return_zero_when_no_key_matches()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 202, Name = "Existing" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.BulkUpdateAsync(
         [new MySqlTestEntity { Id = 999, Name = "Missing" }],
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      affectedRows.Should().Be(0);
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Existing");
   }

   [Fact]
   public async Task Should_return_zero_for_empty_input()
   {
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkUpdateAsync(
         Array.Empty<MySqlTestEntity>(),
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_match_on_a_non_primary_property()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 211, Name = "Match" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.BulkUpdateAsync(
         [new MySqlTestEntity { Id = 212, Name = "Match" }],
         propertiesToUpdate: entity => entity.Id,
         propertiesToMatchOn: entity => entity.Name);

      affectedRows.Should().Be(1);
      (await context.Entities.AsNoTracking().Select(entity => entity.Id).SingleAsync()).Should().Be(212);
   }

   [Fact]
   public async Task Should_update_nullable_column_to_null()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 213, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkUpdateAsync(
         [new MySqlTestEntity { Id = 213, Name = null }],
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().BeNull();
   }

   [Fact]
   public async Task Should_update_using_a_composite_primary_key()
   {
      await using var context = await CreateContextAsync();
      context.CompositeEntities.Add(new MySqlCompositeEntity { RegionId = 7, ItemId = 11, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.BulkUpdateAsync(
         [new MySqlCompositeEntity { RegionId = 7, ItemId = 11, Name = "After" }],
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => new { entity.RegionId, entity.ItemId });

      affectedRows.Should().Be(1);
      (await context.CompositeEntities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("After");
   }

   [Fact]
   public async Task Should_drop_staging_table_when_create_fails_after_the_server_created_it()
   {
      var interceptor = new FailAfterStagingCreateInterceptor();
      Configure = builder => builder.AddInterceptors(interceptor);
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 203, Name = "Before" });
      await ArrangeDbContext.SaveChangesAsync();
      var entity = new MySqlTestEntity { Id = 203, Name = "After" };

      // A temporary table lives as long as the session, so the connection stays open between the two calls.
      await ActDbContext.Database.OpenConnectionAsync();

      interceptor.IsEnabled = true;
      await ActDbContext.Awaiting(ctx => ctx.BulkUpdateAsync([entity])).Should().ThrowAsync<OperationCanceledException>();
      interceptor.IsEnabled = false;

      (await ActDbContext.BulkUpdateAsync([entity])).Should().Be(1);
   }

   [Fact]
   public async Task Should_keep_existing_temp_table_when_staging_create_fails_with_1050()
   {
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 204, Name = "Before" });
      await ArrangeDbContext.SaveChangesAsync();

      // A temporary table lives as long as the session, so the connection stays open.
      await ActDbContext.Database.OpenConnectionAsync();

      // The dispose releases the leased name but keeps the table, so the staging table of the update gets the same name.
      string tableName;

      await using (var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync([new MySqlTestEntity { Id = 1, Name = "Kept" }],
                                                                                   new MySqlTempTableBulkInsertOptions { DropTableOnDispose = false }))
      {
         tableName = tempTable.Name;
      }

      var exception = await ActDbContext.Awaiting(ctx => ctx.BulkUpdateAsync([new MySqlTestEntity { Id = 204, Name = "After" }]))
                                        .Should().ThrowAsync<MySqlException>();

      exception.Which.Number.Should().Be(1050);
      (await ActDbContext.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{tableName}`").ToListAsync()).Should().Equal("Kept");
   }

   private sealed class FailAfterStagingCreateInterceptor : DbCommandInterceptor
   {
      public bool IsEnabled { get; set; }

      public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
      {
         if (IsEnabled && command.CommandText.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal))
            throw new OperationCanceledException("Simulated cancellation after the server created the staging table.");

         return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
      }
   }
}
