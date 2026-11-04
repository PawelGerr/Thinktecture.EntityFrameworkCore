namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkInsertTests : MySqlIntegrationTestsBase
{
   public MySqlBulkInsertTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_selected_columns()
   {
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkInsertAsync(
         [new MySqlTestEntity { Id = 101, Name = "First" }, new MySqlTestEntity { Id = 102, Name = "Second" }],
         entity => new { entity.Id, entity.Name });

      affectedRows.Should().Be(2);
      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToArrayAsync())
         .Should().Equal("First", "Second");
   }

   [Fact]
   public async Task Should_return_zero_for_empty_input()
   {
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkInsertAsync(Array.Empty<MySqlTestEntity>());

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_when_unique_index_is_duplicated()
   {
      var act = async () =>
      {
         await using var context = await CreateContextAsync();
         context.Entities.Add(new MySqlTestEntity { Id = 151, Name = "Unique" });
         await context.SaveChangesAsync();
         context.ChangeTracker.Clear();

         await context.BulkInsertAsync([new MySqlTestEntity { Id = 152, Name = "Unique" }], entity => new { entity.Id, entity.Name });
      };

      (await act.Should().ThrowAsync<MySqlConnector.MySqlException>()).Which.Number.Should().Be(1062);
   }

   [Fact]
   public async Task Should_insert_in_multiple_batches()
   {
      await using var context = await CreateContextAsync();
      var entities = Enumerable.Range(1, 501)
                               .Select(id => new MySqlTestEntity { Id = id, Name = $"Row-{id}" })
                               .ToArray();

      var affectedRows = await context.BulkInsertAsync(entities, entity => new { entity.Id, entity.Name });

      affectedRows.Should().Be(501);
      (await context.Entities.CountAsync()).Should().Be(501);
   }

   [Fact]
   public async Task Should_throw_when_primary_key_is_duplicated()
   {
      var act = async () =>
      {
         await using var context = await CreateContextAsync();
         context.Entities.Add(new MySqlTestEntity { Id = 150, Name = "Existing" });
         await context.SaveChangesAsync();
         context.ChangeTracker.Clear();

         await context.BulkInsertAsync([new MySqlTestEntity { Id = 150, Name = "Duplicate" }], entity => new { entity.Id, entity.Name });
      };

      (await act.Should().ThrowAsync<MySqlConnector.MySqlException>()).Which.Number.Should().Be(1062);
   }
}
