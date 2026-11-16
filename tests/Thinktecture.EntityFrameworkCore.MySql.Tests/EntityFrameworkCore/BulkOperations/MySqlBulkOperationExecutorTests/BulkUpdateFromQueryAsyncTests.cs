using System.Globalization;
using System.Text.RegularExpressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; one test covers the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// "UPDATE target AS t INNER JOIN source AS s ON keys SET ... [WHERE filter]" is executed through the relational command pipeline of EF Core (EfCoreValueExpressionTranslator.ExecuteNonQueryAsync), so the statement is visible in ExecutedCommands.
// ExecutedCommands contains the setup statements of the tests as well, so the tests pick the statement by its prefix.
// - The number of affected rows is the number of matched rows (MySqlConnector requests the found rows), i.e. rows with unchanged values are counted as well.
// - A single key is compared with "=", so a row with a NULL key never matches. A composite key gets the null compensation of EF Core, so two NULL parts match.
// - A MySQL temp table can be referenced once per statement, which is all that the join needs. The target table can be the source table as well,
//   unless the source query contains a subquery on the target table (error 1093).
// - A single statement is atomic, i.e. a failing statement leaves the target table unchanged.
// - The temp table of the source is not available inside a TransactionScope (MySQL XA transactions reject temp tables),
//   so the tests with a transaction use regular tables as the source.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkUpdateFromQueryAsyncTests : MySqlIntegrationTestsBase
{
   private const string _REDIRECT_TABLE = "TestEntities_QueryUpdateRedirect";

   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("5B9587A3-2312-43DF-9681-38EC22AD8606");
   private static readonly Guid _MISSING_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");
   private static readonly ConvertibleClass _staticConvertibleClass = new(42);

   private static string StaticName => "StaticValue";

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkUpdateFromQueryAsyncTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_update_entities_from_temp_table()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName" },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 20 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("Updated1", "Updated2");
      loadedEntities.Select(e => e.Count).Should().Equal(10, 20);
   }

   [Fact]
   public async Task Should_update_multiple_properties()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "OrigReq", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "UpdatedReq", Count = 42 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.RequiredName, (e, f) => f.RequiredName)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("Updated");
      loadedEntity.RequiredName.Should().Be("UpdatedReq");
      loadedEntity.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_update_only_the_rows_with_matching_keys()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName" },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Untouched", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName" },
                                                          new TestEntity { Id = _MISSING_TEST_ENTITY_ID, Name = "Missing", RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("Untouched", "Updated1");
   }

   [Fact]
   public async Task Should_return_0_if_no_matching_rows()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _MISSING_TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName", Count = 99 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Original");
   }

   [Fact]
   public async Task Should_count_matched_rows_whose_values_do_not_change()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "OtherName", RequiredName = "RequiredName", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Changed", RequiredName = "RequiredName", Count = 2 });

      // MySqlConnector requests the number of found rows instead of the number of changed rows.
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(2);
   }

   [Fact]
   public async Task Should_return_0_if_temp_table_is_empty()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync<TestEntity>();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Original");
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_return_0_if_source_query_returns_no_rows(bool useConstantPredicate)
   {
      await ArrangeQueryRowsAsync();

      var sourceQuery = useConstantPredicate
                           ? ActDbContext.QuerySources.Where(s => false)
                           : ActDbContext.QuerySources.Where(s => s.Count > 100);

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(sourceQuery, t => t.Id, s => s.Id, builder => builder.Set(t => t.Name, (t, s) => s.Name));

      affectedRows.Should().Be(0);
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("Target");
   }

   [Fact]
   public async Task Should_generate_correct_sql_shape()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName" });

      await ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Name, (e, f) => f.Name));

      // MySQL has no "UPDATE ... FROM": the source is joined and the keys are part of the join condition.
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().StartWith($"UPDATE `TestEntities` AS `t` INNER JOIN `{tempTable.Name}` AS `t0` ON `t`.`Id` = `t0`.`Id` SET `t`.`Name` = `t0`.`Name`");
      statement.Should().NotContain(" FROM ");
      statement.Should().NotContain(" WHERE ");
   }

   [Fact]
   public async Task Should_update_from_regular_dbset_query()
   {
      await ArrangeQueryRowsAsync();

      var affectedRows = await UpdateQueryTargetsAsync();

      affectedRows.Should().Be(1);

      var loadedTarget = await AssertDbContext.QueryTargets.SingleAsync();
      loadedTarget.Name.Should().Be("Source");
      loadedTarget.Count.Should().Be(2);
      // The aliases are derived from the names of the entities.
      GetExecutedStatement("UPDATE ").Should().MatchRegex(@"^UPDATE `QueryTargets` AS `(\w+)` INNER JOIN `QuerySources` AS `(\w+)` ON `\1`\.`Id` = `\2`\.`Id` SET ");
   }

   [Fact]
   public async Task Should_update_from_regular_dbset_with_different_column_names()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      ArrangeDbContext.TestEntitiesWithDifferentColumnNames.Add(new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "FromSource", Count = 42 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithDifferentColumnNames,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("FromSource");
      loadedEntity.Count.Should().Be(42);

      GetExecutedStatement("UPDATE ").Should().Contain("ON `t`.`Id` = `t0`.`entity_id` SET `t`.`Name` = `t0`.`display_name`, `t`.`Count` = `t0`.`item_count`");
   }

   [Fact]
   public async Task Should_update_target_with_different_column_names()
   {
      ArrangeDbContext.TestEntitiesWithDifferentColumnNames.Add(new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Original", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName", Count = 42 });

      var affectedRows = await ActDbContext.TestEntitiesWithDifferentColumnNames.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDifferentColumnNames.SingleAsync();
      loadedEntity.Name.Should().Be("Updated");
      loadedEntity.Count.Should().Be(42);

      GetExecutedStatement("UPDATE ").Should().Contain("ON `t`.`entity_id` = `t0`.`Id` SET `t`.`display_name` = `t0`.`Name`, `t`.`item_count` = `t0`.`Count`");
   }

   [Fact]
   public async Task Should_update_from_projection_of_the_same_table_by_nullable_foreign_key()
   {
      var parent = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Parent", Count = 99 };
      var child = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Child", ParentId = _TEST_ENTITY_ID, Count = 0 };
      var orphan = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "Orphan", ParentId = null, Count = 0 };
      ArrangeDbContext.TestEntities.AddRange(parent, child, orphan);
      await ArrangeDbContext.SaveChangesAsync();

      // The source rows come from the table that is updated.
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Select(e => new { e.Id, e.Count }),
                            e => e.ParentId,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => e.RequiredName).Should().Equal("Child", "Orphan", "Parent");
      loadedEntities.Select(e => e.Count).Should().Equal(99, 0, 99);
   }

   [Fact]
   public async Task Should_update_from_the_same_table_as_source()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "First", Count = 1 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Second", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count + 10));

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => e.Count).Should().Equal(11, 12);
   }

   [Fact]
   public async Task Should_update_entity_with_composite_primary_key_from_the_same_table()
   {
      ArrangeDbContext.CompositeEntities.AddRange(new MySqlCompositeEntity { RegionId = 1, ItemId = 1, Name = "1-1" },
                                                  new MySqlCompositeEntity { RegionId = 1, ItemId = 2, Name = "1-2" },
                                                  new MySqlCompositeEntity { RegionId = 2, ItemId = 1, Name = "2-1" },
                                                  new MySqlCompositeEntity { RegionId = 2, ItemId = 3, Name = "2-3" });
      await ArrangeDbContext.SaveChangesAsync();

      // Copies the names of region 1 to the rows of region 2 with the same item.
      var sourceQuery = ActDbContext.CompositeEntities.Where(e => e.RegionId == 1).Select(e => new { RegionId = e.RegionId + 1, e.ItemId, e.Name });

      var affectedRows = await ActDbContext.CompositeEntities.BulkUpdateAsync(
                            sourceQuery,
                            e => new { e.RegionId, e.ItemId },
                            f => new { f.RegionId, f.ItemId },
                            builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.CompositeEntities.OrderBy(e => e.RegionId).ThenBy(e => e.ItemId).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("1-1", "1-2", "1-1", "2-3");
   }

   [Fact]
   public async Task Should_apply_parameters_of_the_source_query()
   {
      ArrangeDbContext.QueryTargets.AddRange(new MySqlQueryTarget { Id = 1, Name = "Target 1", Count = 0 },
                                             new MySqlQueryTarget { Id = 2, Name = "Target 2", Count = 0 },
                                             new MySqlQueryTarget { Id = 3, Name = "Target 3", Count = 0 },
                                             new MySqlQueryTarget { Id = 4, Name = "Target 4", Count = 0 });
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "Source 1", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = "Source 2", Count = 5 },
                                             new MySqlQuerySource { Id = 3, Name = "Source 3", Count = 6 },
                                             new MySqlQuerySource { Id = 4, Name = "Source 4", Count = 9 });
      await ArrangeDbContext.SaveChangesAsync();
      var minCount = 5;
      var take = 2;

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                            ActDbContext.QuerySources.Where(s => s.Count >= minCount).OrderBy(s => s.Count).Take(take),
                            t => t.Id,
                            s => s.Id,
                            builder => builder.Set(t => t.Name, (t, s) => s.Name));

      affectedRows.Should().Be(2);

      var loadedTargets = await AssertDbContext.QueryTargets.OrderBy(t => t.Id).ToListAsync();
      loadedTargets.Select(t => t.Name).Should().Equal("Target 1", "Source 2", "Source 3", "Target 4");
   }

   [Fact]
   public async Task Should_update_with_composite_key_join()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "RequiredName", Count = 0 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "RequiredName", Count = 42 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "WrongName", RequiredName = "RequiredName", Count = 99 });

      // Join on both Id and Name: only the first entity matches
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.Name },
                            f => new { f.Id, f.Name },
                            builder => builder.Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.Count).Should().Equal(42, 0);
      // A composite key gets the null compensation of EF Core, so two NULL parts match.
      GetExecutedStatement("UPDATE ").Should().Contain("ON (`t`.`Id` = `t0`.`Id`) AND ((`t`.`Name` = `t0`.`Name`) OR (`t`.`Name` IS NULL AND (`t0`.`Name` IS NULL)))");
   }

   [Fact]
   public async Task Should_not_match_rows_with_null_keys()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Target 1", NullableCount = 1, Count = 0 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Target 2", NullableCount = null, Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "Source 1", NullableCount = 1, Count = 10 },
                                                          new TestEntity { Id = _MISSING_TEST_ENTITY_ID, RequiredName = "Source 2", NullableCount = null, Count = 20 });

      // NULL = NULL is not true, so the keys are not matched null-safe (unlike the entity-based bulk update).
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.NullableCount,
                            f => f.NullableCount,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => e.Count).Should().Equal(10, 0);
   }

   [Fact]
   public async Task Should_update_with_key_of_converted_type()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Target 1", ConvertibleClass = new ConvertibleClass(1), Count = 0 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Target 2", ConvertibleClass = new ConvertibleClass(2), Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "Source", ConvertibleClass = new ConvertibleClass(1), Count = 10 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.ConvertibleClass,
                            f => f.ConvertibleClass,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => e.Count).Should().Equal(10, 0);
   }

   [Fact]
   public async Task Should_update_entities_in_table_name_override()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName", Count = 99 });

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder
                                          .Set(e => e.Name, (e, f) => f.Name)
                                          .Set(e => e.Count, (e, f) => f.Count),
                               options: new MySqlBulkUpdateFromQueryOptions { TableName = _REDIRECT_TABLE });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("UpdatedName");
         (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Original", "the table of the model is not changed");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_from_ForRootEntity_provider()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName", Count = 99 });

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder
                                          .Set(e => e.Name, (e, f) => f.Name)
                                          .Set(e => e.Count, (e, f) => f.Count),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("UpdatedName");
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "Count")).Should().Equal(99);
         (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Original", "the table of the model is not changed");
         GetExecutedStatement("UPDATE ").Should().StartWith($"UPDATE `{_REDIRECT_TABLE}` AS `t` INNER JOIN `{tempTable.Name}` AS `t0` ON ");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_of_the_database_from_ForRootEntity_provider_if_SchemaBehavior_is_Ignore()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database (here: the current one).
      // SchemaBehavior Ignore drops the schema; the default (Throw) rejects it.
      ConfigureMySql = builder => builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
      var database = await ActDbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() AS `Value`").SingleAsync();
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" });

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Name, (e, f) => f.Name),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE, database) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("UpdatedName");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_from_table_name_provider()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" });
         var provider = new SuffixTableNameProvider("_QueryUpdateRedirect");

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Name, (e, f) => f.Name),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = provider });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("UpdatedName");
         provider.RequestedEntityTypes.Should().ContainSingle().Which.ClrType.Should().Be(typeof(TestEntity));
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_escape_table_name_of_table_name_provider()
   {
      const string tableName = "TestEntities`; DROP TABLE TestEntities; -- Redirect";
      await CreateRedirectTableAsync(tableName);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" });

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Name, (e, f) => f.Name),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tableName) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(tableName, "Name")).Should().Equal("UpdatedName");
      }
      finally
      {
         await DropTablesAsync(tableName);
      }
   }

   [Fact]
   public async Task Should_not_write_to_the_current_database_if_the_schema_of_the_table_name_names_another_database()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" });

         // With the default SchemaBehavior (Throw) the provider would drop the schema and update the table of the same name in the current database.
         var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                      tempTable.Query,
                      e => e.Id,
                      f => f.Id,
                      builder => builder.Set(e => e.Name, (e, f) => f.Name),
                      options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE, "other_database") });

         await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*SchemaBehavior*");
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("Redirected");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_read_the_source_rows_from_the_table_of_the_model_if_the_target_table_is_overridden()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Live", RequiredName = "RequiredName", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(_REDIRECT_TABLE)} SELECT * FROM `TestEntities`");
         await ActDbContext.Database.ExecuteSqlRawAsync($"UPDATE {Quote(_REDIRECT_TABLE)} SET `Name` = 'Archive', `Count` = 0");

         // Source: the table of the model. Target: its copy.
         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               ActDbContext.TestEntities,
                               e => e.Id,
                               f => f.Id,
                               builder => builder
                                          .Set(e => e.Name, (e, f) => f.Name)
                                          .Set(e => e.Count, (e, f) => f.Count),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("Live");
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "Count")).Should().Equal(5);
         (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Live");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_not_change_string_values_that_contain_the_name_of_the_table_of_the_model_if_the_target_table_is_overridden()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Name, (e, f) => "`TestEntities`"),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("`TestEntities`");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_throw_if_no_set_entries()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one*");
   }

   [Fact]
   public async Task Should_throw_if_key_count_mismatch()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                         tempTable.Query,
                         e => new TestKey { Id = e.Id, Name = e.Name },
                         f => new TestKey { Id = f.Id },
                         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*number of target key*");
   }

   [Fact]
   public async Task Should_throw_if_target_property_is_not_mapped()
   {
      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                         ActDbContext.TestEntities,
                         e => e.Id,
                         f => f.Id,
                         builder => builder.Set(e => e.Parent, (e, f) => f.Parent));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*'Parent' is not a mapped property*");
   }

   [Fact]
   public async Task Should_throw_if_target_selector_is_not_a_simple_property_access()
   {
      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                         ActDbContext.TestEntities,
                         e => e.Id,
                         f => f.Id,
                         builder => builder.Set(e => e.Name!.Length, (e, f) => f.Count));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Only simple property access*");
   }

   [Fact]
   public async Task Should_throw_if_value_expression_cannot_be_translated()
   {
      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                         ActDbContext.TestEntities,
                         e => e.Id,
                         f => f.Id,
                         builder => builder.Set(e => e.Name, (e, f) => NotTranslatable(f.Name)));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Could not translate value expression*");
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_error_1093_When_source_query_has_a_subquery_on_the_target_table(bool useContains)
   {
      ArrangeDbContext.AddRange(new TestEntity { Id = new Guid("7A000000-0000-0000-0000-00000000001A"), RequiredName = "RequiredName", Count = 1 },
                                new TestEntity { Id = new Guid("7A000000-0000-0000-0000-00000000001B"), RequiredName = "RequiredName", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();
      var context = ActDbContext;

      // MySQL merges the derived table of the source into the UPDATE, and the subquery then reads the update target.
      var source = useContains
                      ? context.TestEntities.Where(s => context.TestEntities.Select(o => o.Id).Contains(s.Id))
                      : context.TestEntities.Where(s => context.TestEntities.Any(o => o.Count > s.Count));
      var act = () => context.TestEntities.BulkUpdateAsync(source, e => e.Id, s => s.Id, builder => builder.Set(e => e.Count, (e, s) => s.Count + 10));

      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1093);
      (await AssertDbContext.TestEntities.AsNoTracking().Select(e => e.Count).ToListAsync()).Should().BeEquivalentTo([1, 2]);
   }

   [Fact]
   public async Task Should_update_only_filtered_target_rows()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 1 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 0 });

      // Only update target rows where Count > 0
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count > 0);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
   }

   [Fact]
   public async Task Should_return_0_when_filter_excludes_all_rows()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName", Count = 0 });

      // Filter excludes all rows (Count > 100, but actual is 0)
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count > 100);

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("Original");
   }

   [Fact]
   public async Task Should_handle_parameterized_filter()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 10 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 3 });
      var threshold = 5;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count > threshold);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
   }

   [Fact]
   public async Task Should_generate_correct_sql_with_filter()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName", Count = 1 });

      await ActDbContext.TestEntities.BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > 0);

      // The filter is a WHERE clause after the SET clause, the keys stay in the join condition.
      GetExecutedStatement("UPDATE ").Should().EndWith("ON `t`.`Id` = `t0`.`Id` SET `t`.`Name` = `t0`.`Name` WHERE `t`.`Count` > 0");
   }

   [Fact]
   public async Task Should_combine_filter_with_composite_key_join()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "RequiredName", Count = 10 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "UpdatedReq1", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "UpdatedReq2", Count = 0 });

      // Composite key join + filter: only the first entity matches the filter (Count > 0)
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.Name },
                            f => new { f.Id, f.Name },
                            builder => builder.Set(e => e.RequiredName, (e, f) => f.RequiredName),
                            filter: (e, f) => e.Count > 0);

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.RequiredName).Should().Equal("UpdatedReq1", "RequiredName");
   }

   [Fact]
   public async Task Should_filter_using_source_property()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 0 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 0 });

      // Only update rows where the source Count > 0
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => f.Count > 0);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
   }

   [Fact]
   public async Task Should_filter_using_both_target_and_source_properties()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 5 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 20 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 10 });

      // Only update where target Count < source Count (first: 5 < 10 = true, second: 20 < 10 = false)
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count < f.Count);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
      GetExecutedStatement("UPDATE ").Should().EndWith("WHERE `t`.`Count` < `t0`.`Count`");
   }

   [Fact]
   public async Task Should_filter_with_arithmetic_on_both()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 10 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated1", RequiredName = "RequiredName", Count = 6 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", RequiredName = "RequiredName", Count = 5 });

      // Only update where target + source Count > 15 (first: 10 + 6 = 16 > 15 = true, second: 3 + 5 = 8 > 15 = false)
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count + f.Count > 15);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
   }

   [Fact]
   public async Task Should_filter_with_string_contains()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 0 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "KeepThis", RequiredName = "RequiredName", Count = 10 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "SkipThis", RequiredName = "RequiredName", Count = 20 });

      // Only update rows where source Name contains "Keep"
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count),
                            filter: (e, f) => f.Name!.Contains("Keep"));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.Count).Should().Equal(10, 0);
   }

   [Fact]
   public async Task Should_update_with_constant_value()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => 42));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(42);
   }

   [Fact]
   public async Task Should_update_with_null()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", NullableCount = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => null)
                                       .Set(e => e.NullableCount, (e, f) => f.NullableCount));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().BeNull();
      loadedEntity.NullableCount.Should().BeNull();
   }

   [Fact]
   public async Task Should_update_with_captured_variable()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });
      var newName = "CapturedValue";

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => newName));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("CapturedValue");
   }

   [Fact]
   public async Task Should_update_with_captured_string_with_special_characters_as_is()
   {
      await ArrangeQueryRowsAsync();
      var newName = "O'Reilly \\ \"quoted\" `tick`; DROP TABLE QueryTargets; -- \\'";

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                            ActDbContext.QuerySources,
                            t => t.Id,
                            s => s.Id,
                            builder => builder.Set(t => t.Name, (t, s) => newName));

      affectedRows.Should().Be(1);
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be(newName);
   }

   [Fact]
   public async Task Should_update_with_captured_string_containing_curly_braces_as_is()
   {
      await ArrangeQueryRowsAsync();
      var newName = """{"key": "value"} {0} {1} {{}}""";

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                            ActDbContext.QuerySources,
                            t => t.Id,
                            s => s.Id,
                            builder => builder.Set(t => t.Name, (t, s) => newName));

      affectedRows.Should().Be(1);
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be(newName);
   }

   [Fact]
   public async Task Should_update_with_captured_string_that_looks_like_a_format_placeholder_as_is_if_the_source_query_has_parameters()
   {
      await ArrangeQueryRowsAsync();
      var minCount = 1;

      var affectedRows = await ActDbContext.QueryTargets.BulkUpdateAsync(
                            ActDbContext.QuerySources.Where(s => s.Count >= minCount),
                            t => t.Id,
                            s => s.Id,
                            builder => builder.Set(t => t.Name, (t, s) => "{0}"));

      affectedRows.Should().Be(1);
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("{0}");
   }

   [Fact]
   public async Task Should_update_with_static_member_value()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => StaticName));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("StaticValue");
   }

   [Fact]
   public async Task Should_update_with_static_member_of_converted_type()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.ConvertibleClass, (e, f) => _staticConvertibleClass));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.ConvertibleClass).SingleAsync()).Should().Be(new ConvertibleClass(42));
   }

   [Fact]
   public async Task Should_update_with_captured_variable_of_converted_type()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });
      var newValue = new ConvertibleClass(43);

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.ConvertibleClass, (e, f) => newValue));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.ConvertibleClass).SingleAsync()).Should().Be(new ConvertibleClass(43));
   }

   [Fact]
   public async Task Should_update_with_captured_values_of_different_types()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = _TEST_ENTITY_ID, Amount = 1m, CreatedAt = new DateTime(2000, 1, 1), Status = MySqlTestStatus.Draft, Flag = false },
                                              new MySqlTypedEntity { Id = _OTHER_TEST_ENTITY_ID, Amount = 2m, CreatedAt = new DateTime(2000, 1, 1), Status = MySqlTestStatus.Draft, Flag = true });
      await ArrangeDbContext.SaveChangesAsync();
      var amount = 1234.5678m;
      var payload = "[1, 2, 3]";
      var createdAt = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified);
      var data = new byte[] { 1, 2, 3 };

      // Only the row with Flag = true is part of the source.
      var affectedRows = await ActDbContext.TypedEntities.BulkUpdateAsync(
                            ActDbContext.TypedEntities.Where(e => e.Flag),
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Amount, (e, f) => amount)
                                       .Set(e => e.Payload, (e, f) => payload)
                                       .Set(e => e.CreatedAt, (e, f) => createdAt)
                                       .Set(e => e.Status, (e, f) => MySqlTestStatus.Published)
                                       .Set(e => e.Data, (e, f) => data)
                                       .Set(e => e.Flag, (e, f) => false));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TypedEntities.OrderBy(e => e.Amount).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[0].Amount.Should().Be(1m);
      loadedEntities[0].Status.Should().Be(MySqlTestStatus.Draft);
      loadedEntities[1].Id.Should().Be(_OTHER_TEST_ENTITY_ID);
      loadedEntities[1].Amount.Should().Be(amount);
      loadedEntities[1].Payload.Should().Be(payload);
      loadedEntities[1].CreatedAt.Should().Be(createdAt);
      loadedEntities[1].Status.Should().Be(MySqlTestStatus.Published);
      loadedEntities[1].Data.Should().Equal(data);
      loadedEntities[1].Flag.Should().BeFalse();
   }

   [Fact]
   public async Task Should_update_with_captured_datetime_with_fractional_seconds()
   {
      ArrangeDbContext.TypedEntities.Add(new MySqlTypedEntity { Id = _TEST_ENTITY_ID, Amount = 1m, CreatedAt = new DateTime(2000, 1, 1), Status = MySqlTestStatus.Draft });
      await ArrangeDbContext.SaveChangesAsync();
      var createdAt = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified).AddTicks(1234560); // the column is datetime(6): 123456 microseconds

      var affectedRows = await ActDbContext.TypedEntities.BulkUpdateAsync(
                            ActDbContext.TypedEntities,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.CreatedAt, (e, f) => createdAt));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TypedEntities.Select(e => e.CreatedAt).SingleAsync()).Should().Be(createdAt);
   }

   [Theory]
   [InlineData(nameof(MySqlTemporalEntity.DateTime))]
   [InlineData(nameof(MySqlTemporalEntity.DateTimeOffset))]
   [InlineData(nameof(MySqlTemporalEntity.TimeOnly))]
   [InlineData(nameof(MySqlTemporalEntity.TimeSpan))]
   [InlineData(nameof(MySqlTemporalEntity.NullableDateTime))]
   [InlineData(nameof(MySqlTemporalEntity.Converted))]
   public async Task Should_update_with_captured_temporal_value_with_fractional_seconds(string column)
   {
      ArrangeDbContext.TemporalEntities.Add(new MySqlTemporalEntity
                                            {
                                               Id = _TEST_ENTITY_ID,
                                               DateTime = new DateTime(2000, 1, 1),
                                               DateTimeOffset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                               TimeOnly = new TimeOnly(1, 0),
                                               TimeSpan = TimeSpan.FromHours(1),
                                               NullableDateTime = new DateTime(2000, 1, 1),
                                               Converted = new MySqlDateTimeWrapper(new DateTime(2000, 1, 1))
                                            });
      await ArrangeDbContext.SaveChangesAsync();

      // The columns have 6 fractional digits: 123456 microseconds.
      var expected = new MySqlTemporalEntity
                     {
                        DateTime = new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234560),
                        DateTimeOffset = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero).AddTicks(1234560),
                        TimeOnly = new TimeOnly(7, 8, 9).Add(TimeSpan.FromTicks(1234560)),
                        TimeSpan = new TimeSpan(7, 8, 9).Add(TimeSpan.FromTicks(1234560)),
                        NullableDateTime = new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234560),
                        Converted = new MySqlDateTimeWrapper(new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234560))
                     };

      var affectedRows = await ActDbContext.TemporalEntities.BulkUpdateAsync(
                            ActDbContext.TemporalEntities,
                            e => e.Id,
                            f => f.Id,
                            builder => column switch
                            {
                               nameof(MySqlTemporalEntity.DateTime) => builder.Set(e => e.DateTime, (e, f) => expected.DateTime),
                               nameof(MySqlTemporalEntity.DateTimeOffset) => builder.Set(e => e.DateTimeOffset, (e, f) => expected.DateTimeOffset),
                               nameof(MySqlTemporalEntity.TimeOnly) => builder.Set(e => e.TimeOnly, (e, f) => expected.TimeOnly),
                               nameof(MySqlTemporalEntity.TimeSpan) => builder.Set(e => e.TimeSpan, (e, f) => expected.TimeSpan),
                               nameof(MySqlTemporalEntity.NullableDateTime) => builder.Set(e => e.NullableDateTime, (e, f) => expected.NullableDateTime),
                               _ => builder.Set(e => e.Converted, (e, f) => expected.Converted)
                            });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TemporalEntities.SingleAsync();
      object loadedValue = column switch
      {
         nameof(MySqlTemporalEntity.DateTime) => loadedEntity.DateTime,
         nameof(MySqlTemporalEntity.DateTimeOffset) => loadedEntity.DateTimeOffset,
         nameof(MySqlTemporalEntity.TimeOnly) => loadedEntity.TimeOnly,
         nameof(MySqlTemporalEntity.TimeSpan) => loadedEntity.TimeSpan,
         nameof(MySqlTemporalEntity.NullableDateTime) => loadedEntity.NullableDateTime!,
         _ => loadedEntity.Converted
      };
      object expectedValue = column switch
      {
         nameof(MySqlTemporalEntity.DateTime) => expected.DateTime,
         nameof(MySqlTemporalEntity.DateTimeOffset) => expected.DateTimeOffset,
         nameof(MySqlTemporalEntity.TimeOnly) => expected.TimeOnly,
         nameof(MySqlTemporalEntity.TimeSpan) => expected.TimeSpan,
         nameof(MySqlTemporalEntity.NullableDateTime) => expected.NullableDateTime!,
         _ => expected.Converted
      };
      loadedValue.Should().Be(expectedValue);
   }

   [Theory]
   [InlineData("1.12:00:00.1234560")]
   [InlineData("-01:30:00.1234560")]
   public async Task Should_update_with_captured_time_span_beyond_one_day_or_negative(string text)
   {
      var value = TimeSpan.Parse(text, CultureInfo.InvariantCulture);
      await AddTemporalEntityAsync(TimeSpan.FromHours(1));

      var affectedRows = await ActDbContext.TemporalEntities.BulkUpdateAsync(
                            ActDbContext.TemporalEntities,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.TimeSpan, (e, f) => value));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TemporalEntities.SingleAsync()).TimeSpan.Should().Be(value);
   }

   [Theory]
   [InlineData("1.12:00:00.1234560")]
   [InlineData("-01:30:00.1234560")]
   public async Task Should_filter_by_captured_time_span_beyond_one_day_or_negative(string text)
   {
      var value = TimeSpan.Parse(text, CultureInfo.InvariantCulture);
      await AddTemporalEntityAsync(value);

      var affectedRows = await ActDbContext.TemporalEntities.BulkUpdateAsync(
                            ActDbContext.TemporalEntities,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.DateTime, (e, f) => new DateTime(2024, 5, 6)),
                            (e, f) => e.TimeSpan == value);

      affectedRows.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_error_1525_for_captured_time_span_outside_the_range_of_TIME()
   {
      var value = TimeSpan.FromHours(839);
      await AddTemporalEntityAsync(TimeSpan.FromHours(1));

      var exception = await ActDbContext.TemporalEntities
                                        .Awaiting(set => set.BulkUpdateAsync(set, e => e.Id, f => f.Id, builder => builder.Set(e => e.TimeSpan, (e, f) => value)))
                                        .Should().ThrowAsync<MySqlException>();

      exception.Which.Number.Should().Be(1525);
   }

   private async Task AddTemporalEntityAsync(TimeSpan timeSpan)
   {
      ArrangeDbContext.TemporalEntities.Add(new MySqlTemporalEntity
                                            {
                                               Id = _TEST_ENTITY_ID,
                                               DateTime = new DateTime(2000, 1, 1),
                                               DateTimeOffset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                               TimeOnly = new TimeOnly(1, 0),
                                               TimeSpan = timeSpan,
                                               NullableDateTime = new DateTime(2000, 1, 1),
                                               Converted = new MySqlDateTimeWrapper(new DateTime(2000, 1, 1))
                                            });
      await ArrangeDbContext.SaveChangesAsync();
   }

   [Fact]
   public async Task Should_translate_datetime_now_server_side_instead_of_evaluating_it()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => DateTime.Now.Year));

      affectedRows.Should().Be(1);

      // DateTime.Now must not be evaluated to a constant at translation time but be translated server-side.
      GetExecutedStatement("UPDATE ").Should().ContainAny("CURRENT_TIMESTAMP", "NOW(");

      // The server evaluates the year with its own clock and time zone, which is at most one day away from UTC.
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().BeInRange(DateTime.UtcNow.AddDays(-1).Year, DateTime.UtcNow.AddDays(1).Year);
   }

   [Fact]
   public async Task Should_translate_datetime_utcnow_server_side_instead_of_evaluating_it()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => DateTime.UtcNow.Year));

      affectedRows.Should().Be(1);

      GetExecutedStatement("UPDATE ").Should().Contain("UTC_TIMESTAMP");

      // The server evaluates the year with its own clock and time zone, which is at most one day away from UTC.
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().BeInRange(DateTime.UtcNow.AddDays(-1).Year, DateTime.UtcNow.AddDays(1).Year);
   }

   [Fact]
   public async Task Should_update_with_source_arithmetic_expression()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 7 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count + 10));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(17);
   }

   [Fact]
   public async Task Should_update_with_mixed_target_and_source_expression()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 3 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(8);
   }

   [Fact]
   public async Task Should_update_with_multiplication_and_constant()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 4 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => f.Count * 3));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(12);
   }

   [Fact]
   public async Task Should_update_with_multiple_complex_and_simple_sets()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 10 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Updated", RequiredName = "RequiredName", Count = 5 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => e.Count + f.Count * 2));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("Updated");
      loadedEntity.Count.Should().Be(20);
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID, Name = "Updated", Count = 42 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder
                                       .Set(e => e.Name, (e, f) => f.Name)
                                       .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("Updated");
      loadedEntity.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_generate_correct_sql_with_source_temp_table_having_different_column_names()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID, Name = "Updated", Count = 42 });

      await ActDbContext.TestEntities.BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder
                    .Set(e => e.Name, (e, f) => f.Name)
                    .Set(e => e.Count, (e, f) => f.Count));

      // Source columns use the mapped names (the alias of the temp table entity is derived from its name, so it is not part of the expectation),
      // target columns use the names of the target (which are not mapped differently).
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().MatchRegex(@"ON `t`\.`Id` = `[^`]+`\.`entity_id` SET `t`\.`Name` = `[^`]+`\.`display_name`, `t`\.`Count` = `[^`]+`\.`item_count`$");
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_using_arithmetic_expression()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID, Name = "Updated", Count = 3 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(8);
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_filter()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original1", RequiredName = "RequiredName", Count = 5 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Original2", RequiredName = "RequiredName", Count = 20 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID, Name = "Updated1", Count = 10 },
                                                          new TempSourceWithMappedColumns { Id = _OTHER_TEST_ENTITY_ID, Name = "Updated2", Count = 10 });

      // Only update where target Count < source Count (first: 5 < 10 = true, second: 20 < 10 = false)
      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name),
                            filter: (e, f) => e.Count < f.Count);

      affectedRows.Should().Be(1);
      await AssertNamesAsync("Updated1", "Original2");
      GetExecutedStatement("UPDATE ").Should().MatchRegex(@"WHERE `t`\.`Count` < `[^`]+`\.`item_count`$");
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_constant_value()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Count, (e, f) => 42));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(42);
   }

   [Fact]
   public async Task Should_update_with_string_to_upper()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "updated", RequiredName = "RequiredName", Count = 0 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => f.Name!.ToUpper()));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("UPDATED");
   }

   [Fact]
   public async Task Should_update_with_string_concat_and_to_lower()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Hello", RequiredName = "World", Count = 0 });

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            tempTable.Query,
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Name, (e, f) => (f.Name + "_" + f.RequiredName).ToLower()));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("hello_world");
   }

   [Fact]
   public async Task Should_not_change_any_row_if_the_statement_fails()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "A" },
                                         new MySqlTestEntity { Id = 2, Name = "B" },
                                         new MySqlTestEntity { Id = 3, Name = "C" });
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "Changed" },
                                             new MySqlQuerySource { Id = 2, Name = "C" });
      await ArrangeDbContext.SaveChangesAsync();

      // 1062: the second row would get the name of the third row, which violates the unique index of MySqlTestEntity.Name
      var exception = await ActDbContext.Entities
                                        .Awaiting(set => set.BulkUpdateAsync(ActDbContext.QuerySources,
                                                                             e => e.Id,
                                                                             s => s.Id,
                                                                             builder => builder.Set(e => e.Name, (e, s) => s.Name)))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);

      (await AssertDbContext.Entities.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync()).Should().Equal("A", "B", "C");
   }

   [Fact]
   public async Task Should_throw_when_cancellation_is_requested()
   {
      await ArrangeQueryRowsAsync();
      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      var act = () => ActDbContext.QueryTargets.BulkUpdateAsync(
                         ActDbContext.QuerySources,
                         t => t.Id,
                         s => s.Id,
                         builder => builder.Set(t => t.Name, (t, s) => s.Name),
                         cancellationToken: cancellationTokenSource.Token);

      await act.Should().ThrowAsync<OperationCanceledException>();
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("Target");
   }

   [Fact]
   public async Task Should_not_end_the_transaction_of_the_caller_and_commit_rows_with_it()
   {
      await ArrangeQueryRowsAsync();
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await UpdateQueryTargetsAsync();

      affectedRows.Should().Be(1);
      (await AssertDbContext.QueryTargets.AsNoTracking().Select(t => t.Name).SingleAsync()).Should().Be("Target", "the transaction of the caller is not committed yet");

      await transaction.CommitAsync();

      (await AssertDbContext.QueryTargets.AsNoTracking().Select(t => t.Name).SingleAsync()).Should().Be("Source");
   }

   [Fact]
   public async Task Should_not_update_rows_if_the_transaction_of_the_caller_is_rolled_back()
   {
      await ArrangeQueryRowsAsync();
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await UpdateQueryTargetsAsync();

      affectedRows.Should().Be(1);

      await transaction.RollbackAsync();

      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("Target");
   }

   [Fact]
   public async Task Should_update_rows_in_the_nested_transaction_of_the_caller()
   {
      await ArrangeQueryRowsAsync();
      await using var rootTransaction = await ActDbContext.Database.BeginTransactionAsync();
      await using var nestedTransaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await UpdateQueryTargetsAsync();

      affectedRows.Should().Be(1);

      // A nested transaction is logical only; the root transaction decides about the rows.
      await nestedTransaction.CommitAsync();
      (await AssertDbContext.QueryTargets.AsNoTracking().Select(t => t.Name).SingleAsync()).Should().Be("Target");

      await rootTransaction.RollbackAsync();
      (await AssertDbContext.QueryTargets.AsNoTracking().Select(t => t.Name).SingleAsync()).Should().Be("Target");
   }

   [Fact]
   public async Task Should_update_and_commit_inside_a_transaction_scope()
   {
      await ArrangeQueryRowsAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await UpdateQueryTargetsAsync();

         affectedRows.Should().Be(1);
         scope.Complete();
      }

      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("Source");
   }

   [Fact]
   public async Task Should_roll_back_the_update_if_the_transaction_scope_is_not_completed()
   {
      await ArrangeQueryRowsAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await UpdateQueryTargetsAsync();

         affectedRows.Should().Be(1);
      }

      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be("Target");
   }

   [Fact]
   public async Task Should_update_struct_ComplexType_members_with_composite_key_join()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder
                                       .Set(e => e.EffectiveMessage.ReceivedAt, (e, s) => s.EffectiveMessage.ReceivedAt)
                                       .Set(e => e.EffectiveMessage.MessageTimestamp, (e, s) => s.EffectiveMessage.MessageTimestamp));

      affectedRows.Should().Be(1);
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().Contain("`t`.`effective_received_at` = `#`.`effective_received_at`");
      statement.Should().Contain("`t`.`effective_message_timestamp` = `#`.`effective_message_timestamp`");

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(100, 200), "Original");
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_with_composite_key_join()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder
                                       .Set(e => e.EffectiveMessage, (e, s) => s.EffectiveMessage)
                                       .Set(e => e.Name, (e, s) => "updated"));

      affectedRows.Should().Be(1);
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().Contain("`t`.`effective_received_at` = `#`.`effective_received_at`");
      statement.Should().Contain("`t`.`effective_message_timestamp` = `#`.`effective_message_timestamp`");

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(100, 200), "updated");
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_when_selector_is_converted_to_object()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

      // Boxing the struct adds a Convert node to both selectors.
      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder.Set<object>(e => e.EffectiveMessage, (e, s) => s.EffectiveMessage));

      affectedRows.Should().Be(1);

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(100, 200), "Original");
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_from_captured_variable()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

      var value = new TimestampPair(7, 8);

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      affectedRows.Should().Be(1);

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(7, 8), "Original");
   }

   [Fact]
   public async Task Should_skip_generated_member_when_setting_whole_ComplexType()
   {
      ConfigureModel = builder => builder.Entity<TestEntityWithComplexType>().ComplexProperty(e => e.Boundary, c => c.Property(b => b.Lower).ValueGeneratedOnAddOrUpdate());
      await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO `TestEntities_with_ComplexType` (`Id`, `Boundary_Upper`, `Boundary_Lower`) VALUES ('{_TEST_ENTITY_ID}', 1, 2)");
      var value = new BoundaryValueObject(10, 20);

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
                            ActDbContext.Set<TestEntityWithComplexType>(),
                            e => e.Id,
                            f => f.Id,
                            builder => builder.Set(e => e.Boundary, (e, f) => value));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("Boundary_Upper").And.NotContain("Boundary_Lower");
      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(2);
   }

   [Fact]
   public async Task Should_throw_when_every_member_of_whole_ComplexType_is_generated()
   {
      ConfigureModel = builder => builder.Entity<TestEntityWithComplexType>().ComplexProperty(e => e.Boundary, c =>
                                                                                              {
                                                                                                 c.Property(b => b.Upper).ValueGeneratedOnAddOrUpdate();
                                                                                                 c.Property(b => b.Lower).ValueGeneratedOnAddOrUpdate();
                                                                                              });
      var value = new BoundaryValueObject(10, 20);

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
                   ActDbContext.Set<TestEntityWithComplexType>(),
                   e => e.Id,
                   f => f.Id,
                   builder => builder.Set(e => e.Boundary, (e, f) => value));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*every member*generated*Expression: e => e.Boundary");
      ExecutedCommands.Should().NotContain(command => command.StartsWith("UPDATE ", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_update_value_converted_members_of_struct_ComplexType()
   {
      ConfigureModel = TempTableWithConvertedComplexType.Configure;
      await ArrangeConvertedComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateConvertedComplexTypeSource());

      var affectedRows = await ActDbContext.TestEntitiesWithConvertedComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder
                                       .Set(e => e.EffectiveMessage.ReceivedAt, (e, s) => s.EffectiveMessage.ReceivedAt)
                                       .Set(e => e.EffectiveMessage.MessageTimestamp, (e, s) => s.EffectiveMessage.MessageTimestamp));

      affectedRows.Should().Be(1);

      await AssertConvertedComplexTypeEntitiesAsync(new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)));
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_with_value_converted_members()
   {
      ConfigureModel = TempTableWithConvertedComplexType.Configure;
      await ArrangeConvertedComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateConvertedComplexTypeSource());

      var affectedRows = await ActDbContext.TestEntitiesWithConvertedComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder.Set(e => e.EffectiveMessage, (e, s) => s.EffectiveMessage));

      affectedRows.Should().Be(1);

      await AssertConvertedComplexTypeEntitiesAsync(new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)));
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_with_value_converted_members_from_captured_variable()
   {
      ConfigureModel = TempTableWithConvertedComplexType.Configure;
      await ArrangeConvertedComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateConvertedComplexTypeSource());

      var value = new ConvertedTimestampPair(new TimestampValue(7), new TimestampValue(8));

      var affectedRows = await ActDbContext.TestEntitiesWithConvertedComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      affectedRows.Should().Be(1);
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().Contain("`effective_received_at` = 7");
      statement.Should().Contain("`effective_message_timestamp` = 8");

      await AssertConvertedComplexTypeEntitiesAsync(value);
   }

   [Fact]
   public async Task Should_update_struct_ComplexType_in_table_from_ForRootEntity_provider()
   {
      const string redirectTable = "TestEntitiesWithStructComplexType_Redirect";
      ConfigureModel = TempTableWithStructComplexType.Configure;

      await CreateTableCopyAsync("TestEntitiesWithStructComplexType", redirectTable);

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(redirectTable)} (`id`, `message_timestamp`, `effective_received_at`, `effective_message_timestamp`, `Name`) VALUES ('{_TEST_ENTITY_ID}', 10, 1, 2, NULL)");
         await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

         var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                               tempTable.Query,
                               e => new { e.Id, e.MessageTimestamp },
                               s => new { s.Id, s.MessageTimestamp },
                               builder => builder
                                          .Set(e => e.EffectiveMessage.ReceivedAt, (e, s) => s.EffectiveMessage.ReceivedAt)
                                          .Set(e => e.EffectiveMessage.MessageTimestamp, (e, s) => s.EffectiveMessage.MessageTimestamp),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(redirectTable) });

         affectedRows.Should().Be(1);

         var columnSum = await AssertDbContext.Database
                                              .SqlQueryRaw<long>($"SELECT `effective_received_at` * 1000 + `effective_message_timestamp` AS `Value` FROM {Quote(redirectTable)}")
                                              .SingleAsync();
         columnSum.Should().Be(100_200);
      }
      finally
      {
         await DropTablesAsync(redirectTable);
      }
   }

   [Fact]
   public async Task Should_update_whole_nested_struct_ComplexType()
   {
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = _TEST_ENTITY_ID, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         await using var tempTable = await CreateSourceAsync(new TestEntityWithNestedStructComplexType { Id = _TEST_ENTITY_ID, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } });

         var affectedRows = await ActDbContext.Set<TestEntityWithNestedStructComplexType>().BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               s => s.Id,
                               builder => builder.Set(e => e.Envelope, (e, s) => s.Envelope));

         affectedRows.Should().Be(1);

         var loaded = await AssertDbContext.Set<TestEntityWithNestedStructComplexType>().SingleAsync();
         loaded.Envelope.Should().Be(new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 });
      }
      finally
      {
         await DropTablesAsync(TestEntityWithNestedStructComplexType.TableName);
      }
   }

   [Fact]
   public async Task Should_update_nested_struct_ComplexType_member()
   {
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = _TEST_ENTITY_ID, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         await using var tempTable = await CreateSourceAsync(new TestEntityWithNestedStructComplexType { Id = _TEST_ENTITY_ID, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } });

         var affectedRows = await ActDbContext.Set<TestEntityWithNestedStructComplexType>().BulkUpdateAsync(
                               tempTable.Query,
                               e => e.Id,
                               s => s.Id,
                               builder => builder.Set(e => e.Envelope.Key.ReceivedAt, (e, s) => s.Envelope.Key.ReceivedAt + e.Envelope.Sequence));

         affectedRows.Should().Be(1);

         var loaded = await AssertDbContext.Set<TestEntityWithNestedStructComplexType>().SingleAsync();
         loaded.Envelope.Should().Be(new MessageEnvelope { Key = new TimestampPair(103, 2), Sequence = 3 });
      }
      finally
      {
         await DropTablesAsync(TestEntityWithNestedStructComplexType.TableName);
      }
   }

   [Fact]
   public async Task Should_throw_when_EF_Property_is_called_on_complex_property()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntityWithComplexType(_TEST_ENTITY_ID, new BoundaryValueObject(10, 20)));

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => EF.Property<int>(e.Boundary, "Upper"), (e, f) => f.Boundary.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("EF.Property is supported only on the lambda parameter, not through a complex property, a navigation or another expression.*");
   }

   [Fact]
   public async Task Should_throw_when_whole_ComplexType_and_member_are_both_set()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntityWithComplexType(_TEST_ENTITY_ID, new BoundaryValueObject(10, 20)));

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Boundary, (e, f) => f.Boundary).Set(e => e.Boundary.Upper, (e, f) => 42));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The target column 'Boundary_Upper' is set more than once.*");
   }

   [Fact]
   public async Task Should_throw_when_complex_member_is_not_mapped()
   {
      // MessageEnvelope has no constructor with parameters, therefore EF Core can build the model without the ignored member.
      ConfigureModel = builder =>
                       {
                          TestEntityWithNestedStructComplexType.Configure(builder);
                          builder.Entity<TestEntityWithNestedStructComplexType>().ComplexProperty(e => e.Envelope, c => c.Ignore(nameof(MessageEnvelope.Sequence)));
                       };

      var act = () => ActDbContext.Set<TestEntityWithNestedStructComplexType>().BulkUpdateAsync(ActDbContext.Set<TestEntityWithNestedStructComplexType>(), e => e.Id, f => f.Id, builder => builder.Set(e => e.Envelope.Sequence, (e, f) => 1L));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The property 'Sequence' is not a mapped property on '*Envelope#MessageEnvelope'.");
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_source_columns()
   {
      var toValues = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0001"));
      var toNulls = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0002"));
      ArrangeDbContext.AddRange(toValues, toNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceRows = new[] { TestEntityWithNullableComplexTypes.CreateWithValues(toValues.Id), TestEntityWithNullableComplexTypes.CreateWithNulls(toNulls.Id) };
      await using var tempTable = await CreateSourceAsync(sourceRows);

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            tempTable.Query, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.OptionalBoundary, (e, s) => s.OptionalBoundary)
                                              .Set(e => e.OptionalPair, (e, s) => s.OptionalPair)
                                              .Set(e => e.Tagged, (e, s) => s.Tagged)
                                              .Set(e => e.Outer, (e, s) => s.Outer)
                                              .Set(e => e.OptionalOuter, (e, s) => s.OptionalOuter)
                                              .Set(e => e.OptionalConverted, (e, s) => s.OptionalConverted));

      affectedRows.Should().Be(2);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync();
      loaded.Should().BeEquivalentTo(sourceRows);

      GetExecutedStatement("UPDATE ").Should().Contain("`Tagged_Discriminator` = ").And.NotContain("CASE");
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_captured_values()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0003"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var expected = TestEntityWithNullableComplexTypes.CreateWithValues(entity.Id);
      var boundary = expected.OptionalBoundary;
      var pair = expected.OptionalPair;
      var tagged = expected.Tagged;
      var outer = expected.Outer;
      var optionalOuter = expected.OptionalOuter;
      var converted = expected.OptionalConverted;

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary)
                                              .Set(e => e.OptionalPair, (e, s) => pair)
                                              .Set(e => e.Tagged, (e, s) => tagged)
                                              .Set(e => e.Outer, (e, s) => outer)
                                              .Set(e => e.OptionalOuter, (e, s) => optionalOuter)
                                              .Set(e => e.OptionalConverted, (e, s) => converted));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).Should().BeEquivalentTo(expected);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_captured_nulls()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0004"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      BoundaryValueObject? boundary = null;
      TimestampPair? pair = null;
      TaggedValues? tagged = null;
      var outer = new OuterWithOptionalInner { Z = 1, Inner = null };
      OptionalOuter? optionalOuter = null;
      ConvertedTimestampPair? converted = null;

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary)
                                              .Set(e => e.OptionalPair, (e, s) => pair)
                                              .Set(e => e.Tagged, (e, s) => tagged)
                                              .Set(e => e.Outer, (e, s) => outer)
                                              .Set(e => e.OptionalOuter, (e, s) => optionalOuter)
                                              .Set(e => e.OptionalConverted, (e, s) => converted));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).Should().BeEquivalentTo(TestEntityWithNullableComplexTypes.CreateWithNulls(entity.Id));
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_new_expression()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0005"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.OptionalOuter, (e, s) => new OptionalOuter { Y = 3, Inner = new InnerValue { V = 33 } })
                                              .Set(e => e.Tagged, (e, s) => new TaggedValues { X = 7, Text = "t" }));

      affectedRows.Should().Be(1);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      loaded.OptionalOuter.Should().BeEquivalentTo(new OptionalOuter { Y = 3, Inner = new InnerValue { V = 33 } });
      loaded.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 7, Text = "t" });
   }

   [Fact]
   public async Task Should_set_whole_ComplexTypes_from_server_conditional()
   {
      var rowA = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0020"));
      var rowB = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0021"));
      ArrangeDbContext.AddRange(rowA, rowB);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceA = TestEntityWithNullableComplexTypes.CreateWithValues(rowA.Id);
      var sourceB = TestEntityWithNullableComplexTypes.CreateWithNulls(rowB.Id);
      sourceB.Outer.Z = 0;
      await using var tempTable = await CreateSourceAsync(sourceA, sourceB);

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            tempTable.Query, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Tagged, (e, s) => s.Outer.Z > 0 ? s.Tagged : e.Tagged)
                                              .Set(e => e.OptionalPair, (e, s) => s.Outer.Z > 0 ? s.OptionalPair : e.OptionalPair));

      affectedRows.Should().Be(2);
      var loadedA = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowA.Id);
      loadedA.Tagged.Should().BeEquivalentTo(sourceA.Tagged);
      loadedA.OptionalPair.Should().Be(sourceA.OptionalPair);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowB.Id)).Should().BeEquivalentTo(rowB);

      GetExecutedStatement("UPDATE ").Should().Contain("`Tagged_Discriminator` = CASE")
                                     .And.Contain("`Tagged_X` = CASE")
                                     .And.Contain("`OptionalPair_ReceivedAt` = CASE");
   }

   [Fact]
   public async Task Should_set_whole_ComplexTypes_from_client_conditional_with_non_null_captured_value()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0022"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      TaggedValues? tagged = new TaggedValues { X = 7, Text = "t" };
      TimestampPair? pair = new TimestampPair(100, 200);
      OuterWithOptionalInner? outer = new OuterWithOptionalInner { Z = 1, Inner = new InnerValue { V = 11 } };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Tagged, (e, s) => tagged == null ? s.Tagged : tagged)
                                              .Set(e => e.OptionalPair, (e, s) => pair == null ? s.OptionalPair : pair)
                                              .Set(e => e.Outer, (e, s) => outer == null ? s.Outer : outer));

      affectedRows.Should().Be(1);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      loaded.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 7, Text = "t" });
      loaded.OptionalPair.Should().Be(new TimestampPair(100, 200));
      loaded.Outer.Should().BeEquivalentTo(new OuterWithOptionalInner { Z = 1, Inner = new InnerValue { V = 11 } });

      GetExecutedStatement("UPDATE ").Should().NotContain("CASE").And.Contain("`Tagged_Discriminator` = ");
   }

   [Fact]
   public async Task Should_set_whole_struct_ComplexType_from_captured_nullable_value()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await CreateSourceAsync(CreateStructComplexTypeSource());

      TimestampPair? value = new TimestampPair(7, 8);

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                            tempTable.Query,
                            e => new { e.Id, e.MessageTimestamp },
                            s => new { s.Id, s.MessageTimestamp },
                            builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      affectedRows.Should().Be(1);

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(7, 8), "Original");
   }

   [Fact]
   public async Task Should_throw_for_captured_null_nullable_value_of_required_struct_ComplexType()
   {
      TimestampPair? value = null;

      var act = () => ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
                   ActDbContext.TestEntitiesWithStructComplexType,
                   e => e.Id,
                   s => s.Id,
                   builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The member 'Value' of '*' cannot be read because the captured value is null.*");
      ExecutedCommands.Should().NotContain(command => command.StartsWith("UPDATE ", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_set_whole_nullable_struct_ComplexTypes_when_selectors_are_converted_to_object()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0006"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      TimestampPair? pair = new TimestampPair(7, 8);
      ConvertedTimestampPair? converted = null;

      // Boxing the nullable struct adds a Convert node to both selectors.
      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
                            builder => builder.Set<object?>(e => e.OptionalPair, (e, s) => pair)
                                              .Set<object?>(e => e.OptionalConverted, (e, s) => converted));

      affectedRows.Should().Be(1);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      loaded.OptionalPair.Should().Be(new TimestampPair(7, 8));
      loaded.OptionalConverted.Should().BeNull();
   }

   [Fact]
   public async Task Should_throw_for_member_of_nullable_class_ComplexType()
   {
      var act = () => ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id, builder => builder.Set(e => e.OptionalBoundary!.Upper, (e, s) => s.OptionalBoundary!.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The member 'OptionalBoundary' of '*' is a nullable complex property.*");
      ExecutedCommands.Should().NotContain(command => command.StartsWith("UPDATE ", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_throw_for_member_of_nullable_struct_ComplexType()
   {
      var act = () => ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id, builder => builder.Set(e => e.OptionalPair!.Value.ReceivedAt, (e, s) => s.OptionalPair!.Value.ReceivedAt));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The member 'OptionalPair' of '*' is a nullable complex property.*");
      ExecutedCommands.Should().NotContain(command => command.StartsWith("UPDATE ", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_throw_for_whole_complex_collection()
   {
      ConfigureModel = TestEntityWithComplexCollection.Configure;

      var act = () => ActDbContext.Set<TestEntityWithComplexCollection>().BulkUpdateAsync(ActDbContext.Set<TestEntityWithComplexCollection>(), e => e.Id, s => s.Id, builder => builder.Set(e => e.Boundaries, (e, s) => s.Boundaries));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The complex collection 'Boundaries' is not supported.*");
   }

   [Fact]
   public async Task Should_update_no_rows_when_filter_short_circuits_to_false()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "A", RequiredName = "RequiredName" },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "B", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 5 },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 5 });
      TestEntity? captured = null;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count), filter: (e, f) => captured != null && e.Name == captured.Name);

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.Select(e => e.Count).ToListAsync()).Should().Equal(0, 0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_compares_column_with_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0001"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0002"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Where(s => s.Name == null), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_compares_column_with_not_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0003"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0004"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Where(s => s.Name != null), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NOT NULL").And.NotContain("<> NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(0);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(1);
   }

   [Fact]
   public async Task Should_update_rows_When_filter_compares_column_with_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0005"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0006"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => s.Name == null);

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_set_value_from_conditional_When_test_compares_column_with_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0007"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0008"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => s.Name == null ? 1 : 2));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(2);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_compares_column_with_captured_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0009"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A000A"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      string? captured = null;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Where(s => s.Name == captured), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_uses_collection_parameter()
   {
      ConfigureMySql = builder => builder.AddCollectionParameterSupport();
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A000B"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A000C"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var ids = new List<Guid> { nullName.Id };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Where(s => ids.Contains(s.Id)), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_filters_nullable_ComplexType()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0010"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0011"));
      ArrangeDbContext.AddRange(withValues, withNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes.Where(s => s.OptionalBoundary != null), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Outer.Z, (e, s) => 5));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("IS NOT NULL");
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == withValues.Id)).Outer.Z.Should().Be(5);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == withNulls.Id)).Outer.Z.Should().Be(1);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_coalesce_with_column_left_operand()
   {
      var rowA = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0027"));
      var rowB = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0028"));
      ArrangeDbContext.AddRange(rowA, rowB);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceA = TestEntityWithNullableComplexTypes.CreateWithNulls(rowA.Id);
      var sourceB = TestEntityWithNullableComplexTypes.CreateWithValues(rowB.Id);
      sourceB.OptionalBoundary = new BoundaryValueObject(3, 9);
      sourceB.Tagged = new TaggedValues { X = 9, Text = "s" };
      await using var tempTable = await CreateSourceAsync(sourceA, sourceB);

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            tempTable.Query, e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.OptionalBoundary, (e, s) => s.OptionalBoundary ?? e.OptionalBoundary)
                                              .Set(e => e.Tagged, (e, s) => s.Tagged ?? e.Tagged));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain("Tagged_Discriminator").And.Contain("IS NULL").And.NotContain("= NULL");
      var loadedA = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowA.Id);
      loadedA.OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(2, 5));
      loadedA.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 7, Text = "t" });
      var loadedB = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowB.Id);
      loadedB.OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(3, 9));
      loadedB.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 9, Text = "s" });
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_compares_value_converted_property_with_captured_value()
   {
      var match = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-000000000001"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(5) };
      var other = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-000000000002"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(6) };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var captured = new ConvertibleClass(5);

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.Where(s => s.ConvertibleClass == captured), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_is_FromSql_with_argument()
   {
      var match = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-000000000003"), RequiredName = "RequiredName", Name = "x" };
      var other = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-000000000004"), RequiredName = "RequiredName", Name = "y" };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var name = "x";

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities.FromSqlRaw("SELECT * FROM `TestEntities` WHERE `Name` = {0}", name), e => e.Id, s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_override_table_When_value_and_filter_use_target_parameter()
   {
      const string targetTable = "TestEntities_QueryUpdateTarget";
      var id = new Guid("8C000000-0000-0000-0000-00000000001E");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", targetTable);

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
                                                         INSERT INTO {Quote(targetTable)} (`Id`, `RequiredName`, `Name`, `Count`, `NullableCount`, `ConvertibleClass`, `ParentId`, `PropertyWithBackingField`, `_privateField`)
                                                         VALUES ('{id}', 'RequiredName', NULL, 100, NULL, NULL, NULL, 0, 0)
                                                         """);

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               ActDbContext.TestEntities.Where(e => e.Id == id),
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count),
                               filter: (e, f) => e.Count >= 100,
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(targetTable) });

         affectedRows.Should().Be(1);

         var statement = GetExecutedStatement("UPDATE ");
         statement.Should().Contain($"`{targetTable}` AS", Exactly.Once());
         statement.Should().Contain("`TestEntities` AS", Exactly.Once());

         (await LoadColumnAsync<int>(targetTable, "Count")).Should().Equal(101);
         (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
      }
      finally
      {
         await DropTablesAsync(targetTable);
      }
   }

   [Fact]
   public async Task Should_update_override_table_from_source_query_over_model_table()
   {
      ArrangeDbContext.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Source", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
                                                         INSERT INTO {Quote(_REDIRECT_TABLE)} (`Id`, `RequiredName`, `Name`, `Count`, `NullableCount`, `ConvertibleClass`, `ParentId`, `PropertyWithBackingField`, `_privateField`)
                                                         VALUES ('{_TEST_ENTITY_ID}', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0)
                                                         """);

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               ActDbContext.TestEntities.Where(e => e.Id == _TEST_ENTITY_ID),
                               e => e.Id,
                               f => f.Id,
                               builder => builder
                                          .Set(e => e.Name, (e, f) => f.Name)
                                          .Set(e => e.Count, (e, f) => f.Count + 40),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);

         var statement = GetExecutedStatement("UPDATE ");
         statement.Should().Contain($"`{_REDIRECT_TABLE}`", Exactly.Once());
         statement.Should().Contain("`TestEntities` AS", Exactly.Once());

         var redirected = await AssertDbContext.Database
                                               .SqlQueryRaw<string>($"SELECT CONCAT(`Name`, ':', `Count`) AS `Value` FROM {Quote(_REDIRECT_TABLE)}")
                                               .SingleAsync();
         redirected.Should().Be("Source:41");

         var source = await AssertDbContext.TestEntities.SingleAsync();
         source.Name.Should().Be("Source");
         source.Count.Should().Be(1);
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_update_table_in_override_schema_from_source_query_over_model_table()
   {
      // MySQL has no schemas below the database; SchemaBehavior Translate maps the schema into the table name ("archive_TestEntities").
      const string archiveSchema = "archive";
      const string archiveTable = $"{archiveSchema}_TestEntities";
      ConfigureMySql = builder => builder.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, name) => $"{schema}_{name}");
      ArrangeDbContext.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Source", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", archiveTable);

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
                                                         INSERT INTO {Quote(archiveTable)} (`Id`, `RequiredName`, `Name`, `Count`, `NullableCount`, `ConvertibleClass`, `ParentId`, `PropertyWithBackingField`, `_privateField`)
                                                         VALUES ('{_TEST_ENTITY_ID}', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0)
                                                         """);

         var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                               ActDbContext.TestEntities.Where(e => e.Id == _TEST_ENTITY_ID),
                               e => e.Id,
                               f => f.Id,
                               builder => builder.Set(e => e.Count, (e, f) => f.Count + 40),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(archiveSchema) });

         affectedRows.Should().Be(1);

         var statement = GetExecutedStatement("UPDATE ");
         statement.Should().Contain(archiveSchema, Exactly.Once());
         statement.Should().Contain($"`{archiveTable}` AS", Exactly.Once());
         statement.Should().Contain("`TestEntities` AS", Exactly.Once());

         (await LoadColumnAsync<int>(archiveTable, "Count")).Should().Equal(41);
         (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
      }
      finally
      {
         await DropTablesAsync(archiveTable);
      }
   }

   [Theory]
   [InlineData(false, 1)]
   [InlineData(true, 0)]
   public async Task Should_update_rows_When_filter_projects_captured_collection_with_lambda(bool emptyCollection, int expectedAffectedRows)
   {
      var match = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A0"), RequiredName = "RequiredName" };
      var other = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A1"), RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var items = emptyCollection ? new List<CapturedItem>() : new List<CapturedItem> { new(match.Id, true), new(Guid.NewGuid(), false) };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => items.Select(i => i.Id).Contains(s.Id));

      affectedRows.Should().Be(expectedAffectedRows);

      if (!emptyCollection)
         GetExecutedStatement("UPDATE ").Should().Contain(" IN (");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(emptyCollection ? 0 : 1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_captured_collection_with_lambda()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("8C000000-0000-0000-0000-00000000000C"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var boundaries = new List<BoundaryValueObject> { new(1, 2) };
      var tagged = new List<TaggedValues> { new() { X = 8, Text = "u" } };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundaries.FirstOrDefault(b => b.Upper == 99))
                                              .Set(e => e.Tagged, (e, s) => tagged.FirstOrDefault(t => t.X == 8)));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("`OptionalBoundary_Upper` = NULL").And.Contain("`Tagged_X` = 8");

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      loaded.OptionalBoundary.Should().BeNull();
      loaded.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 8, Text = "u" });
   }

   [Fact]
   public async Task Should_update_rows_When_value_is_count_of_captured_collection_filtered_with_lambda()
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A2"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var items = new List<CapturedItem> { new(Guid.NewGuid(), true), new(Guid.NewGuid(), false), new(Guid.NewGuid(), true) };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => items.Where(i => items.Any(j => j.Id == i.Id && j.Active)).Count()));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("`Count` = 2");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(2);
   }

   [Theory]
   [InlineData(true, 101)]
   [InlineData(false, 0)]
   public async Task Should_update_rows_When_conditional_test_is_Any_over_captured_collection_with_lambda(bool itemMatches, int expectedCount)
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A3"), RequiredName = "RequiredName", Count = 1 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var probe = Guid.NewGuid();
      var items = new List<CapturedItem> { new(itemMatches ? probe : Guid.NewGuid(), true) };

      await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => items.Any(i => i.Id == probe) ? s.Count + 100 : 0));

      var statement = GetExecutedStatement("UPDATE ");

      if (itemMatches)
      {
         statement.Should().Contain("+ 100");
      }
      else
      {
         statement.Should().Contain("`Count` = 0");
         statement.Should().NotContain("+ 100");
      }

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(expectedCount);
   }

   [Fact]
   public async Task Should_throw_When_lambda_over_captured_collection_uses_source_parameter()
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A4"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var items = new List<CapturedItem> { new(Guid.NewGuid(), true) };

      Func<Task> act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                          ActDbContext.TestEntities,
                          e => e.Id,
                          s => s.Id,
                          builder => builder.Set(e => e.Count, (e, s) => items.Where(i => i.Id == s.Id).Count()));

      var exception = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The LINQ expression 'i => *could not be translated*");
      exception.Which.Message.Should().NotContain("unbound variable");
   }

   [Theory]
   [InlineData(2, 1)]
   [InlineData(1, 1)]
   [InlineData(0, 0)]
   public async Task Should_update_rows_When_filter_uses_captured_Guid_array_Contains(int arrayLength, int expectedAffectedRows)
   {
      var match = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A5"), RequiredName = "RequiredName" };
      var other = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A6"), RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      Guid[] ids = arrayLength switch
      {
         0 => [],
         1 => [match.Id],
         _ => [match.Id, Guid.NewGuid()]
      };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => ids.Contains(s.Id));

      affectedRows.Should().Be(expectedAffectedRows);

      if (arrayLength == 2)
         GetExecutedStatement("UPDATE ").Should().Contain(" IN (");

      if (arrayLength == 1)
         GetExecutedStatement("UPDATE ").Should().NotContain(" IN (");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(expectedAffectedRows);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_string_array_Contains()
   {
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000001A"), RequiredName = "RequiredName", Name = "x" };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000001B"), RequiredName = "RequiredName", Name = "z" };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      string[] names = ["x", "y"];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => names.Contains(s.Name!));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain(" IN ('x', 'y')");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_value_uses_inline_array_Contains()
   {
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000001C"), RequiredName = "RequiredName", Count = 1 };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000001D"), RequiredName = "RequiredName", Count = 3 };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => new[] { 1, 2 }.Contains(s.Count) ? 7 : 8));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain(" IN (1, 2)");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(7);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(8);
   }

   [Fact]
   public async Task Should_update_rows_When_value_uses_captured_int_array_Contains()
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A7"), RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      int[] counts = [5, 7];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => counts.Contains(s.Count) ? s.Count + 100 : 0));

      affectedRows.Should().Be(1);
      var statement = GetExecutedStatement("UPDATE ");
      statement.Should().Contain("CASE");
      statement.Should().Contain(" IN (");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(105);
   }

   [Fact]
   public async Task Should_update_rows_When_captured_array_is_used_inside_captured_lambda()
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A8"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var items = new List<CapturedItem> { new(Guid.NewGuid(), true), new(Guid.NewGuid(), false), new(Guid.NewGuid(), true) };
      Guid[] ids = [items[0].Id, items[1].Id];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => items.Where(i => ids.Contains(i.Id)).Count()));

      affectedRows.Should().Be(1);
      GetExecutedStatement("UPDATE ").Should().Contain("`Count` = 2");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(2);
   }

   [Fact]
   public async Task Should_update_value_converted_property_from_server_conditional_over_captured_values()
   {
      var low = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000001"), RequiredName = "RequiredName", Count = 10 };
      var high = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000002"), RequiredName = "RequiredName", Count = 20 };
      ArrangeDbContext.AddRange(low, high);
      await ArrangeDbContext.SaveChangesAsync();

      var highValue = new ConvertibleClass(2);

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.ConvertibleClass, (e, s) => s.Count > 15 ? highValue : new ConvertibleClass(1)));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain("THEN 2").And.Contain("ELSE 1");

      var stored = await AssertDbContext.Database.SqlQueryRaw<int>("SELECT `ConvertibleClass` AS `Value` FROM `TestEntities` ORDER BY `Count`").ToListAsync();
      stored.Should().Equal(1, 2);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexType_with_value_converted_members_from_server_conditional_over_captured_values()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("8C000000-0000-0000-0000-000000000003"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("8C000000-0000-0000-0000-000000000004"));
      ArrangeDbContext.AddRange(withValues, withNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var forValues = new ConvertedTimestampPair(new TimestampValue(11), new TimestampValue(12));
      var forNulls = new ConvertedTimestampPair(new TimestampValue(21), new TimestampValue(22));

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
                            ActDbContext.TestEntitiesWithNullableComplexTypes,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.OptionalConverted, (e, s) => s.OptionalBoundary != null ? forValues : forNulls));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain("THEN 11").And.Contain("ELSE 21").And.Contain("THEN 12").And.Contain("ELSE 22");

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.OrderBy(e => e.Id).ToListAsync();
      loaded[0].OptionalConverted.Should().Be(forValues);
      loaded[1].OptionalConverted.Should().Be(forNulls);
   }

   [Fact]
   public async Task Should_update_value_converted_int_property_from_server_conditional_over_constants()
   {
      ConfigureModel = builder => builder.Entity<TestEntity>().Property(e => e.Count).HasConversion(v => v * 100, v => v / 100);

      var low = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000001"), RequiredName = "RequiredName", NullableCount = 10 };
      var high = new TestEntity { Id = new Guid("8D000000-0000-0000-0000-000000000002"), RequiredName = "RequiredName", NullableCount = 20 };
      ArrangeDbContext.AddRange(low, high);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => s.NullableCount > 15 ? 2 : 1));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain("THEN 200").And.Contain("ELSE 100");

      var stored = await AssertDbContext.Database.SqlQueryRaw<int>("SELECT `Count` AS `Value` FROM `TestEntities` ORDER BY `NullableCount`").ToListAsync();
      stored.Should().Equal(100, 200);
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_collection_with_null_element()
   {
      var withNull = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000000F"), RequiredName = "RequiredName", NullableCount = null };
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000010"), RequiredName = "RequiredName", NullableCount = 7 };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000011"), RequiredName = "RequiredName", NullableCount = 8 };
      ArrangeDbContext.AddRange(withNull, match, other);
      await ArrangeDbContext.SaveChangesAsync();

      int?[] counts = [7, null];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => counts.Contains(s.NullableCount));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain(".`NullableCount` IS NULL").And.Contain(".`NullableCount` = 7");

      (await AssertDbContext.TestEntities.Where(e => e.Count == 1).CountAsync()).Should().Be(2);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_no_rows_When_filter_uses_captured_null_collection()
   {
      var entity = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000019"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      int[]? ids = null;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => ids!.Contains(s.Count));

      affectedRows.Should().Be(0);
      GetExecutedStatement("UPDATE ").Should().Contain("1 = 0");
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_HashSet_with_null_element()
   {
      var withNull = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000022"), RequiredName = "RequiredName", NullableCount = null };
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000023"), RequiredName = "RequiredName", NullableCount = 7 };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000024"), RequiredName = "RequiredName", NullableCount = 8 };
      ArrangeDbContext.AddRange(withNull, match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var counts = new HashSet<int?> { 7, null };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => 1),
                            filter: (e, s) => counts.Contains(s.NullableCount));

      affectedRows.Should().Be(2);
      GetExecutedStatement("UPDATE ").Should().Contain(".`NullableCount` IS NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_take_else_branch_When_value_uses_Contains_of_captured_null_collection()
   {
      var entity = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000025"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      int[]? ids = null;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
                            ActDbContext.TestEntities,
                            e => e.Id,
                            s => s.Id,
                            builder => builder.Set(e => e.Count, (e, s) => ids!.Contains(s.Count) ? 1 : 2));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(2);
   }

   // Target: { Id = 1, Name = "Target", Count = 1 }, source: { Id = 1, Name = "Source", Count = 2 }
   private async Task ArrangeQueryRowsAsync()
   {
      ArrangeDbContext.QueryTargets.Add(new MySqlQueryTarget { Id = 1, Name = "Target", Count = 1 });
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private Task<int> UpdateQueryTargetsAsync()
   {
      return ActDbContext.QueryTargets.BulkUpdateAsync(
         ActDbContext.QuerySources,
         t => t.Id,
         s => s.Id,
         builder => builder
                    .Set(t => t.Name, (t, s) => s.Name)
                    .Set(t => t.Count, (t, s) => s.Count));
   }

   private async Task AssertNamesAsync(string expectedNameOfFirstEntity, string expectedNameOfSecondEntity)
   {
      var names = await AssertDbContext.TestEntities.ToDictionaryAsync(e => e.Id, e => e.Name);

      names.Should().HaveCount(2);
      names[_TEST_ENTITY_ID].Should().Be(expectedNameOfFirstEntity);
      names[_OTHER_TEST_ENTITY_ID].Should().Be(expectedNameOfSecondEntity);
   }

   private static string NotTranslatable(string? value)
   {
      return value ?? String.Empty;
   }

   private static void ConfigureTempSourceWithMappedColumns(ModelBuilder builder)
   {
      builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
                                                                           {
                                                                              b.HasKey(e => e.Id);
                                                                              b.Property(e => e.Id).HasColumnName("entity_id");
                                                                              b.Property(e => e.Name).HasColumnName("display_name");
                                                                              b.Property(e => e.Count).HasColumnName("item_count");
                                                                           });
   }

   private async Task<ITempTableQuery<T>> CreateSourceAsync<T>(params T[] entities)
      where T : class
   {
      return await SUT.BulkInsertIntoTempTableAsync(entities, new MySqlTempTableBulkInsertOptions());
   }

   // Line breaks of the generated SQL depend on the platform, so whitespace is normalized.
   private string GetExecutedStatement(string prefix)
   {
      return Regex.Replace(ExecutedCommands.Last(command => command.StartsWith(prefix, StringComparison.Ordinal)), @"\s+", " ");
   }

   // Creates a copy of the table "TestEntities" that contains a row with the name "Redirected" and, as a decoy, adds a row with the name "Original"
   // to the table of the model. Both rows have the id of _TEST_ENTITY_ID.
   private async Task CreateRedirectTableAsync(string tableName = _REDIRECT_TABLE)
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await CreateTableCopyAsync("TestEntities", tableName);
      await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(tableName)} SELECT * FROM `TestEntities`");
      await ActDbContext.Database.ExecuteSqlRawAsync($"UPDATE {Quote(tableName)} SET `Name` = 'Redirected'");
   }

   private async Task<List<T>> LoadColumnAsync<T>(string tableName, string columnName)
   {
      return await AssertDbContext.Database.SqlQueryRaw<T>($"SELECT {Quote(columnName)} AS `Value` FROM {Quote(tableName)} ORDER BY {Quote(columnName)}").ToListAsync();
   }

   // LIKE copies the columns, the indexes and the primary key, but no foreign keys.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      // A run that was killed before its cleanup leaves the table behind; CREATE TABLE would then fail with error 1050.
      await DropTablesAsync(targetTableName);
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(targetTableName)} LIKE {Quote(sourceTableName)}");
   }

   private async Task DropTablesAsync(params string[] tableNames)
   {
      foreach (var tableName in tableNames)
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {Quote(tableName)}");
      }
   }

   private static string Quote(string identifier)
   {
      return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
   }

   private async Task ArrangeStructComplexTypeEntitiesAsync()
   {
      ArrangeDbContext.TestEntitiesWithStructComplexType.AddRange(
         new TestEntityWithStructComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = new TimestampPair(1, 2), Name = "Original" },
         new TestEntityWithStructComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 11, EffectiveMessage = new TimestampPair(3, 4), Name = "Untouched" });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private static TempTableWithStructComplexType CreateStructComplexTypeSource()
   {
      return new TempTableWithStructComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = new TimestampPair(100, 200) };
   }

   private async Task AssertStructComplexTypeEntitiesAsync(TimestampPair expectedEffectiveMessage, string expectedName)
   {
      var loaded = await AssertDbContext.TestEntitiesWithStructComplexType.OrderBy(e => e.MessageTimestamp).ToListAsync();
      loaded.Should().BeEquivalentTo(
      [
         new TestEntityWithStructComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = expectedEffectiveMessage, Name = expectedName },
         new TestEntityWithStructComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 11, EffectiveMessage = new TimestampPair(3, 4), Name = "Untouched" }
      ]);
   }

   private async Task ArrangeConvertedComplexTypeEntitiesAsync()
   {
      ArrangeDbContext.TestEntitiesWithConvertedComplexType.AddRange(
         new TestEntityWithConvertedComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2)) },
         new TestEntityWithConvertedComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private static TempTableWithConvertedComplexType CreateConvertedComplexTypeSource()
   {
      return new TempTableWithConvertedComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)) };
   }

   private async Task AssertConvertedComplexTypeEntitiesAsync(ConvertedTimestampPair expectedEffectiveMessage)
   {
      var loaded = await AssertDbContext.TestEntitiesWithConvertedComplexType.OrderBy(e => e.MessageTimestamp).ToListAsync();
      loaded.Should().BeEquivalentTo(
      [
         new TestEntityWithConvertedComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 10, EffectiveMessage = expectedEffectiveMessage },
         new TestEntityWithConvertedComplexType { Id = _TEST_ENTITY_ID, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) }
      ]);
   }

   // The entity is registered through ConfigureModel only, so EnsureCreated of the shared database does not create its table.
   // The Guid column uses the type of the provider (char(36), ascii), like the temp table of the source.
   private async Task CreateNestedStructComplexTypeTableAsync()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
                                                      CREATE TABLE IF NOT EXISTS {Quote(TestEntityWithNestedStructComplexType.TableName)}
                                                      (
                                                         `id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL PRIMARY KEY,
                                                         `sequence` bigint NOT NULL,
                                                         `received_at` bigint NOT NULL,
                                                         `message_timestamp` bigint NOT NULL
                                                      )
                                                      """);
   }

   private sealed class TestKey
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
   }

   private sealed record CapturedItem(Guid Id, bool Active);
}
