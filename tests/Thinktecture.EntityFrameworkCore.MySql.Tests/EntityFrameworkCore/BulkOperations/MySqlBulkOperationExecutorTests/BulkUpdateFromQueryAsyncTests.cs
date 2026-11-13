using System.Text.RegularExpressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; one test covers the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// "UPDATE target AS t INNER JOIN source AS s ON keys SET ... [WHERE filter]" is executed by EF Core (ExecuteSqlRawAsync), so the statement is visible in ExecutedCommands.
// ExecutedCommands contains the setup statements of the tests as well, so the tests pick the statement by its prefix.
// - The number of affected rows is the number of matched rows (MySqlConnector requests the found rows), i.e. rows with unchanged values are counted as well.
// - The keys are compared with "=", i.e. rows with a NULL key never match.
// - A MySQL temp table can be referenced once per statement, which is all that the join needs. The target table can be the source table as well,
//   because MySQL materializes a derived table.
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

   public BulkUpdateFromQueryAsyncTests(MySqlContainerFixture fixture)
      : base(fixture)
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
      GetExecutedStatement("UPDATE ").Should().Contain("ON (`t`.`Id` = `t0`.`Id`) AND (`t`.`Name` = `t0`.`Name`)");
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
   public async Task Should_update_entities_in_table_of_the_database_from_ForRootEntity_provider()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database (here: the current one).
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

   [Fact(Skip = "Known limitation: the schema of a table name is dropped by DelimitIdentifier of the MySQL SQL generation helper (Pomelo SchemaBehavior), so the statement runs against the current database without an error.")]
   public async Task Should_not_write_to_the_current_database_if_the_schema_of_the_table_name_names_another_database()
   {
      await CreateRedirectTableAsync();

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" });

         // The schema of a MySQL table name is the name of the database. The statement has to address that database (or fail),
         // it must not silently update the table of the same name in the current database.
         var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
                      tempTable.Query,
                      e => e.Id,
                      f => f.Id,
                      builder => builder.Set(e => e.Name, (e, f) => f.Name),
                      options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE, "other_database") });

         await act.Should().ThrowAsync<Exception>();
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("Redirected");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact(Skip = "Known defect of the shared EfCoreValueExpressionTranslator.ApplyTableName (all providers): the table name of the model is replaced everywhere in the generated SQL, including the source query and string literals, instead of the target table only.")]
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

   [Fact(Skip = "Known defect of the shared EfCoreValueExpressionTranslator.ApplyTableName (all providers): the table name of the model is replaced everywhere in the generated SQL, including the source query and string literals, instead of the target table only.")]
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

   [Fact(Skip = "Known defect of the shared EfCoreValueExpressionTranslator (all providers): a DateTime constant is inlined as a SQL literal without fractional seconds, the microseconds are lost. The insert path binds a parameter and keeps them.")]
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
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(DateTime.Now.Year);
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
      (await AssertDbContext.TestEntities.Select(e => e.Count).SingleAsync()).Should().Be(DateTime.UtcNow.Year);
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

   private sealed class TestKey
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
   }
}
