namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkInsertOrUpdateTests : MySqlIntegrationTestsBase
{
   public MySqlBulkInsertOrUpdateTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_when_key_does_not_exist()
   {
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 301, Name = "Inserted" }],
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      affectedRows.Should().Be(1);
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Inserted");
   }

   [Fact]
   public async Task Should_update_when_primary_key_conflicts()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 302, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 302, Name = "After" }],
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("After");
   }

   [Fact]
   public async Task Should_leave_unselected_properties_unchanged_on_conflict()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 303, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 303, Name = "After" }],
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => entity.Id,
         propertiesToMatchOn: entity => entity.Id);

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Before");
   }

   [Fact]
   public async Task Should_upsert_using_a_composite_primary_key()
   {
      await using var context = await CreateContextAsync();
      context.CompositeEntities.Add(new MySqlCompositeEntity { RegionId = 8, ItemId = 12, Name = "Before" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkInsertOrUpdateAsync(
         [new MySqlCompositeEntity { RegionId = 8, ItemId = 12, Name = "After" }],
         propertiesToInsert: entity => new { entity.RegionId, entity.ItemId, entity.Name },
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => new { entity.RegionId, entity.ItemId });

      (await context.CompositeEntities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("After");
   }

   [Fact]
   public async Task Should_not_modify_other_row_when_unique_index_conflicts()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 1, Name = "a" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkInsertOrUpdateAsync(
         [new MySqlTestEntity { Id = 9, Name = "a" }],
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => new { entity.Id, entity.Name },
         propertiesToMatchOn: entity => entity.Id);

      var rows = await context.Entities.AsNoTracking().ToListAsync();
      rows.Should().ContainSingle();
      rows[0].Id.Should().Be(1);
      rows[0].Name.Should().Be("a");
   }

   [Fact]
   public async Task Should_upsert_mixed_batch_with_unique_index_conflict()
   {
      await using var context = await CreateContextAsync();
      context.Entities.Add(new MySqlTestEntity { Id = 1, Name = "a" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      await context.BulkInsertOrUpdateAsync(
         [
            new MySqlTestEntity { Id = 9, Name = "a" },
            new MySqlTestEntity { Id = 10, Name = "new" },
            new MySqlTestEntity { Id = 1, Name = "a3" }
         ],
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      var rows = await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => new { entity.Id, entity.Name }).ToListAsync();
      rows.Should().BeEquivalentTo(new[] { new { Id = 1, Name = "a3" }, new { Id = 10, Name = "new" } }, options => options.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_return_zero_for_empty_input()
   {
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkInsertOrUpdateAsync(
         Array.Empty<MySqlTestEntity>(),
         propertiesToInsert: entity => new { entity.Id, entity.Name },
         propertiesToUpdate: entity => entity.Name,
         propertiesToMatchOn: entity => entity.Id);

      affectedRows.Should().Be(0);
   }
}
