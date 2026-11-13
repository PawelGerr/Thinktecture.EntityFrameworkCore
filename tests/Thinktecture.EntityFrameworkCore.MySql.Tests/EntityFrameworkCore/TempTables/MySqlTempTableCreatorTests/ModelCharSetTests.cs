using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;

/// <summary>
/// A model-level character set must only be written for columns that can have one. MySQL rejects <c>json CHARACTER SET ...</c>.
/// </summary>
[Collection(MySqlTestCollection.NAME)]
public sealed class ModelCharSetTests : MySqlIntegrationTestsBase
{
   public ModelCharSetTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
      ConfigureModel = modelBuilder => modelBuilder.HasCharSet("utf8mb4");
   }

   [Fact]
   public async Task Should_update_entities_with_a_json_column_when_the_model_has_a_default_charset()
   {
      var id = Guid.NewGuid();
      ArrangeDbContext.TypedEntities.Add(new MySqlTypedEntity { Id = id, Payload = """{"a":1}""", Status = MySqlTestStatus.Draft, CreatedAt = new DateTime(2020, 1, 2) });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.BulkUpdateAsync(
                                                            [new MySqlTypedEntity { Id = id, Payload = """{"a":2}""", Status = MySqlTestStatus.Published }],
                                                            entity => new { entity.Payload, entity.Status },
                                                            entity => entity.Id);

      affectedRows.Should().Be(1);
      var row = await AssertDbContext.TypedEntities.AsNoTracking().SingleAsync();
      row.Status.Should().Be(MySqlTestStatus.Published);
      row.Payload.Should().Contain("2");
   }

   [Fact]
   public async Task Should_create_a_temp_table_with_a_json_column_when_the_model_has_a_default_charset()
   {
      var id = Guid.NewGuid();
      var executor = ActDbContext.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertIntoTempTableAsync(
                                                                              [new MySqlTypedEntity { Id = id, Payload = """{"a":1}""", Status = MySqlTestStatus.Draft }],
                                                                              executor.CreateOptions(),
                                                                              CancellationToken.None);

      (await tempTable.Query.Select(entity => entity.Id).SingleAsync()).Should().Be(id);
   }

   [Fact]
   public async Task Should_write_the_charset_for_text_columns_only()
   {
      var executor = ActDbContext.GetService<ITempTableBulkInsertExecutor>();

      await using var tempTable = await executor.BulkInsertIntoTempTableAsync(
                                                                              [new MySqlTypedEntity { Id = Guid.NewGuid(), Payload = "{}", Status = MySqlTestStatus.Published }],
                                                                              executor.CreateOptions(),
                                                                              CancellationToken.None);

      var createStatement = ExecutedCommands.Single(command => command.Contains("CREATE TEMPORARY TABLE", StringComparison.Ordinal));

      // A value-converted enum is stored as varchar and still belongs to the model's character set.
      createStatement.Should().MatchRegex(@"`Status` varchar\(\d+\) CHARACTER SET utf8mb4");
      createStatement.Should().NotMatchRegex(@"`Payload` json CHARACTER SET");
   }
}
