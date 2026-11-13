using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkOperationSemanticsTests : MySqlIntegrationTestsBase
{
   public MySqlBulkOperationSemanticsTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_return_the_number_of_processed_rows_for_a_mixed_upsert()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "a" }, new MySqlTestEntity { Id = 2, Name = "b" }]);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync(
         [
            new MySqlTestEntity { Id = 1, Name = "a2" },
            new MySqlTestEntity { Id = 2, Name = "b2" },
            new MySqlTestEntity { Id = 3, Name = "c" },
            new MySqlTestEntity { Id = 4, Name = "d" },
            new MySqlTestEntity { Id = 5, Name = "e" }
         ],
         new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(5);
      (await context.Entities.CountAsync()).Should().Be(5);
   }

   [Fact]
   public async Task Should_return_one_for_an_upsert_without_changes()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "a" }]);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 1, Name = "a" }],
         new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);
   }

   [Fact]
   public async Task Should_not_rewrite_the_primary_key_when_upserting_by_another_key()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "a" }]);

      await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 9, Name = "a" }],
         new MySqlBulkInsertOrUpdateOptions { KeyProperties = IEntityPropertiesProvider.Include<MySqlTestEntity>(entity => entity.Name) });

      (await context.Entities.AsNoTracking().Select(entity => entity.Id).ToListAsync()).Should().Equal(1);
   }

   [Fact]
   public async Task Should_throw_when_a_bulk_update_has_no_property_to_update()
   {
      await using var context = await CreateContextAsync();

      var act = () => context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlTestEntity { Id = 1, Name = "a" }],
         new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTestEntity>(entity => entity.Id) });

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be 0*");
   }

   [Fact]
   public async Task Should_not_renumber_explicit_ids_in_the_staging_table_of_a_bulk_update()
   {
      await using var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `AutoIncrementEntities`");
      context.AutoIncrementEntities.AddRange(new MySqlAutoIncrementEntity { Name = "first" }, new MySqlAutoIncrementEntity { Name = "second" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlAutoIncrementEntity { Id = 0, Name = "changed" }],
         new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlAutoIncrementEntity>(entity => entity.Name) });

      affectedRows.Should().Be(0);
      (await context.AutoIncrementEntities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync())
         .Should().Equal("first", "second");
   }

   [Fact]
   public async Task Should_update_identity_rows_by_their_ids()
   {
      await using var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `AutoIncrementEntities`");
      context.AutoIncrementEntities.AddRange(new MySqlAutoIncrementEntity { Name = "first" }, new MySqlAutoIncrementEntity { Name = "second" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlAutoIncrementEntity { Id = 2, Name = "changed" }],
         new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlAutoIncrementEntity>(entity => entity.Name) });

      affectedRows.Should().Be(1);
      (await context.AutoIncrementEntities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync())
         .Should().Equal("first", "changed");
   }

   [Fact]
   public async Task Should_reset_the_auto_increment_counter_when_truncating_outside_of_a_transaction()
   {
      await using var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `AutoIncrementEntities`");
      context.AutoIncrementEntities.AddRange(new MySqlAutoIncrementEntity { Name = "first" }, new MySqlAutoIncrementEntity { Name = "second" });
      await context.SaveChangesAsync();

      await context.TruncateTableAsync<MySqlAutoIncrementEntity>();
      context.ChangeTracker.Clear();

      var entity = new MySqlAutoIncrementEntity { Name = "after" };
      context.AutoIncrementEntities.Add(entity);
      await context.SaveChangesAsync();
      entity.Id.Should().Be(1);
   }

   [Fact]
   public async Task Should_not_reset_the_auto_increment_counter_when_truncating_inside_a_transaction()
   {
      await using var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `AutoIncrementEntities`");
      context.AutoIncrementEntities.AddRange(new MySqlAutoIncrementEntity { Name = "first" }, new MySqlAutoIncrementEntity { Name = "second" });
      await context.SaveChangesAsync();

      await using (var transaction = await context.Database.BeginTransactionAsync())
      {
         await context.TruncateTableAsync<MySqlAutoIncrementEntity>();
         await transaction.CommitAsync();
      }

      context.ChangeTracker.Clear();
      var entity = new MySqlAutoIncrementEntity { Name = "after" };
      context.AutoIncrementEntities.Add(entity);
      await context.SaveChangesAsync();
      entity.Id.Should().Be(3);
   }
}
