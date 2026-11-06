namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkUpdateTests : MySqlIntegrationTestsBase
{
   public MySqlBulkUpdateTests(MySqlContainerFixture fixture)
      : base(fixture)
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
}
