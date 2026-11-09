using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryAsync : IntegrationTestsBase
{
   private static readonly Guid _structComplexTypeId = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");

   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public BulkUpdateFromQueryAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_update_entities_from_temp_table()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      // Insert updated values into a temp table
      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 20 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Name.Should().Be("Updated1");
      loadedEntities[0].Count.Should().Be(10);
      loadedEntities[1].Name.Should().Be("Updated2");
      loadedEntities[1].Count.Should().Be(20);
   }

   [Fact]
   public async Task Should_update_multiple_properties()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "OrigReq", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", RequiredName = "UpdatedReq", Count = 42 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
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
   public async Task Should_return_0_if_no_matching_rows()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var nonMatchingEntities = new List<TestEntity>
      {
         new() { Id = new Guid("506E664A-9ADC-4221-9577-71DCFD73DE64"), Name = "Updated", RequiredName = "RequiredName", Count = 99 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(nonMatchingEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_generate_correct_sql_shape()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      var lastCommand = ExecutedCommands.Last();
      lastCommand.Should().Contain("UPDATE");
      lastCommand.Should().Contain("SET");
      lastCommand.Should().Contain("FROM");
      lastCommand.Should().Contain("WHERE");
   }

   [Fact]
   public async Task Should_update_from_regular_dbset_query()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity_1);
      await ArrangeDbContext.SaveChangesAsync();

      var entity_with_auto_increment = new TestEntityWithAutoIncrement { Name = "Source" };
      ArrangeDbContext.Add(entity_with_auto_increment);
      await ArrangeDbContext.SaveChangesAsync();

      // Use TestEntityWithAutoIncrement as source to update TestEntity.Name
      // We need a query whose TSource is an entity type that shares properties we can join on
      // For this test, let's use a self-join on TestEntity to update Name from another row's RequiredName
      var entity_2_id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      var entity_2 = new TestEntity { Id = entity_2_id, Name = "NameToUse", RequiredName = "RequiredName", Count = 99 };
      ArrangeDbContext.Add(entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      // Use entity_2 as source to update entity_1's Count
      var sourceQuery = ActDbContext.Set<TestEntity>().Where(e => e.Id == entity_2_id)
                                    .Select(e => new { Identifier = e.Id, Number = e.Count});

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Identifier,
         builder => builder.Set(e => e.Count, (e, f) => f.Number));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync(e => e.Id ==entity_2_id);
      loadedEntity.Count.Should().Be(99);
   }

   [Fact]
   public async Task Should_update_with_composite_key_join()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "RequiredName", Count = 0 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "RequiredName", Count = 42 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "WrongName", RequiredName = "RequiredName", Count = 99 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Join on both Id and Name — only entity_1 matches
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => new { e.Id, e.Name },
         f => new { f.Id, f.Name },
         builder => builder.Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Count.Should().Be(42);

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Count.Should().Be(0); // not updated because Name didn't match
   }

   [Fact]
   public async Task Should_update_entities_in_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryUpdateRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         INSERT INTO "{Schema}"."TestEntities_QueryUpdateRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0);
         """);

      try
      {
         var updatedEntities = new List<TestEntity>
         {
            new()
            {
               Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
               RequiredName = "RequiredName",
               Name = "UpdatedName",
               Count = 99
            }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            sourceQuery,
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count),
            options: new NpgsqlBulkUpdateFromQueryOptions { TableName = "TestEntities_QueryUpdateRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryUpdateRedirect" """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("UpdatedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryUpdateRedirect" """);
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryUpdateRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         INSERT INTO "{Schema}"."TestEntities_QueryUpdateRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0);
         """);

      try
      {
         var updatedEntities = new List<TestEntity>
         {
            new()
            {
               Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
               RequiredName = "RequiredName",
               Name = "UpdatedName",
               Count = 99
            }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            sourceQuery,
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count),
            options: new NpgsqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateRedirect", Schema) });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryUpdateRedirect" """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("UpdatedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryUpdateRedirect" """);
      }
   }

   [Fact]
   public async Task Should_throw_if_no_set_entries()
   {
      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one*");
   }

   [Fact]
   public async Task Should_throw_if_key_count_mismatch()
   {
      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => new TestKey { Id = e.Id, Name = e.Name },
         f => new TestKey { Id = f.Id },
         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*number of target key*");
   }

   [Fact]
   public async Task Should_update_only_filtered_target_rows()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 1 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 1 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Only update target rows where Count > 0
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > 0);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_return_0_when_filter_excludes_all_rows()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", RequiredName = "RequiredName", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Filter excludes all rows (Count > 100, but actual is 0)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > 100);

      affectedRows.Should().Be(0);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("Original");
   }

   [Fact]
   public async Task Should_handle_parameterized_filter()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 10 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 3 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 3 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var threshold = 5;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > threshold);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_generate_correct_sql_with_filter()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName", Count = 1 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", RequiredName = "RequiredName", Count = 1 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > 0);

      var lastCommand = ExecutedCommands.Last();
      lastCommand.Should().Contain("FROM");
      lastCommand.Should().Contain("WHERE");
      lastCommand.Should().Contain("AND");
      lastCommand.Should().Contain("\"Count\"");
   }

   [Fact]
   public async Task Should_combine_filter_with_composite_key_join()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "RequiredName", Count = 10 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "UpdatedReq1", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "UpdatedReq2", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Composite key join + filter: only entity_1 matches the filter (Count > 0)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => new { e.Id, e.Name },
         f => new { f.Id, f.Name },
         builder => builder.Set(e => e.RequiredName, (e, f) => f.RequiredName),
         filter: (e, f) => e.Count > 0);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.RequiredName.Should().Be("UpdatedReq1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.RequiredName.Should().Be("RequiredName");
   }

   [Fact]
   public async Task Should_filter_using_source_property()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 0 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Only update rows where the source Count > 0
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => f.Count > 0);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_filter_using_both_target_and_source_properties()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 5 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 20 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 10 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Only update where target Count < source Count (entity_1: 5 < 10 = true, entity_2: 20 < 10 = false)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count < f.Count);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_filter_with_arithmetic_on_both()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 10 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 3 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", RequiredName = "RequiredName", Count = 6 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", RequiredName = "RequiredName", Count = 5 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      // Only update where target + source Count > 15 (entity_1: 10 + 6 = 16 > 15 = true, entity_2: 3 + 5 = 8 > 15 = false)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count + f.Count > 15);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_update_with_constant_value()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => 42));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_update_with_captured_variable()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var newName = "CapturedValue";

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => newName));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("CapturedValue");
   }

   public static string StaticName => "StaticValue";
   private static readonly ConvertibleClass _staticConvertibleClass = new(42);

   [Fact]
   public async Task Should_update_with_static_member_value()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => StaticName));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("StaticValue");
   }

   [Fact]
   public async Task Should_update_with_static_member_of_converted_type()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.ConvertibleClass, (e, f) => _staticConvertibleClass));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.ConvertibleClass.Should().Be(new ConvertibleClass(42));
   }

   [Fact]
   public async Task Should_update_with_captured_variable_of_converted_type()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var newValue = new ConvertibleClass(43);

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.ConvertibleClass, (e, f) => newValue));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.ConvertibleClass.Should().Be(new ConvertibleClass(43));
   }

   [Fact]
   public async Task Should_translate_datetime_now_server_side_instead_of_evaluating_it()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => DateTime.Now.Year));

      affectedRows.Should().Be(1);

      // DateTime.Now must not be evaluated to a constant at translation time but be translated server-side.
      ExecutedCommands.Last().Should().Contain("now()");

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(DateTime.Now.Year);
   }

   [Fact]
   public async Task Should_update_with_source_arithmetic_expression()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName", Count = 7 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => f.Count + 10));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(17);
   }

   [Fact]
   public async Task Should_update_with_mixed_target_and_source_expression()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName", Count = 3 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(8);
   }

   [Fact]
   public async Task Should_update_with_multiplication_and_constant()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName", Count = 4 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => f.Count * 3));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(12);
   }

   [Fact]
   public async Task Should_update_with_multiple_complex_and_simple_sets()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 10 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", RequiredName = "RequiredName", Count = 5 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => e.Count + f.Count * 2));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("Updated");
      loaded.Count.Should().Be(20);
   }


   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", Count = 42 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("Updated");
      loaded.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_generate_correct_sql_with_source_temp_table_having_different_column_names()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", Count = 42 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => f.Count));

      var lastCommand = ExecutedCommands.Last();
      // Source columns should use mapped names
      lastCommand.Should().Contain("entity_id");
      lastCommand.Should().Contain("display_name");
      lastCommand.Should().Contain("item_count");
      // Target columns should use unmapped (property) names
      lastCommand.Should().Contain("\"Name\"");
      lastCommand.Should().Contain("\"Count\"");
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_using_arithmetic_expression()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", Count = 3 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(8);
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_filter()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 5 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 20 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", Count = 10 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      // Only update where target Count < source Count (entity_1: 5 < 10 = true, entity_2: 20 < 10 = false)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count < f.Count);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_constant_value()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => 42));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_update_with_string_to_upper()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "updated", RequiredName = "RequiredName", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name!.ToUpper()));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("UPDATED");
   }

   [Fact]
   public async Task Should_update_with_string_concat_and_to_lower()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Hello", RequiredName = "World", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => (f.Name + "_" + f.RequiredName).ToLower()));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("hello_world");
   }

   [Fact]
   public async Task Should_filter_with_string_contains()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 0 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "KeepThis", RequiredName = "RequiredName", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "SkipThis", RequiredName = "RequiredName", Count = 20 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new NpgsqlTempTableBulkInsertOptions());

      // Only update rows where source Name contains "Keep"
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => f.Count),
         filter: (e, f) => f.Name!.Contains("Keep"));

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Count.Should().Be(10);

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_struct_ComplexType_members_with_composite_key_join()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
         tempTable.Query,
         e => new { e.Id, e.MessageTimestamp },
         s => new { s.Id, s.MessageTimestamp },
         builder => builder
            .Set(e => e.EffectiveMessage.ReceivedAt, (e, s) => s.EffectiveMessage.ReceivedAt)
            .Set(e => e.EffectiveMessage.MessageTimestamp, (e, s) => s.EffectiveMessage.MessageTimestamp));

      affectedRows.Should().Be(1);
      var sql = ExecutedCommands.Last();
      sql.Should().Contain("effective_received_at = \"#\".effective_received_at");
      sql.Should().Contain("effective_message_timestamp = \"#\".effective_message_timestamp");

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(100, 200), "Original");
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_with_composite_key_join()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
         tempTable.Query,
         e => new { e.Id, e.MessageTimestamp },
         s => new { s.Id, s.MessageTimestamp },
         builder => builder
            .Set(e => e.EffectiveMessage, (e, s) => s.EffectiveMessage)
            .Set(e => e.Name, (e, s) => "updated"));

      affectedRows.Should().Be(1);
      var sql = ExecutedCommands.Last();
      sql.Should().Contain("effective_received_at = \"#\".effective_received_at");
      sql.Should().Contain("effective_message_timestamp = \"#\".effective_message_timestamp");

      await AssertStructComplexTypeEntitiesAsync(new TimestampPair(100, 200), "updated");
   }

   [Fact]
   public async Task Should_update_whole_struct_ComplexType_when_selector_is_converted_to_object()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

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
      await ActDbContext.Database.ExecuteSqlRawAsync($"""INSERT INTO "{Schema}"."TestEntities_with_ComplexType" ("Id", "Boundary_Upper", "Boundary_Lower") VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 1, 2)""");
      var value = new BoundaryValueObject(10, 20);

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         ActDbContext.Set<TestEntityWithComplexType>(),
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary, (e, f) => value));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("Boundary_Upper").And.NotContain("Boundary_Lower");
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
      ExecutedCommands.Should().NotContain(c => c.Contains("UPDATE"));
   }

   [Fact]
   public async Task Should_update_value_converted_members_of_struct_ComplexType()
   {
      ConfigureModel = TempTableWithConvertedComplexType.Configure;
      await ArrangeConvertedComplexTypeEntitiesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateConvertedComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateConvertedComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateConvertedComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

      var value = new ConvertedTimestampPair(new TimestampValue(7), new TimestampValue(8));

      var affectedRows = await ActDbContext.TestEntitiesWithConvertedComplexType.BulkUpdateAsync(
         tempTable.Query,
         e => new { e.Id, e.MessageTimestamp },
         s => new { s.Id, s.MessageTimestamp },
         builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      affectedRows.Should().Be(1);
      var sql = ExecutedCommands.Last();
      sql.Should().Contain("effective_received_at = 7");
      sql.Should().Contain("effective_message_timestamp = 8");

      await AssertConvertedComplexTypeEntitiesAsync(value);
   }

   [Fact]
   public async Task Should_update_struct_ComplexType_in_table_from_ForRootEntity_provider()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntitiesWithStructComplexType_Redirect" (LIKE "{Schema}"."TestEntitiesWithStructComplexType" INCLUDING ALL);
         INSERT INTO "{Schema}"."TestEntitiesWithStructComplexType_Redirect" ("id", "message_timestamp", "effective_received_at", "effective_message_timestamp", "Name")
         VALUES ('{_structComplexTypeId}', 10, 1, 2, NULL);
         """);

      try
      {
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

         var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkUpdateAsync(
            tempTable.Query,
            e => new { e.Id, e.MessageTimestamp },
            s => new { s.Id, s.MessageTimestamp },
            builder => builder
               .Set(e => e.EffectiveMessage.ReceivedAt, (e, s) => s.EffectiveMessage.ReceivedAt)
               .Set(e => e.EffectiveMessage.MessageTimestamp, (e, s) => s.EffectiveMessage.MessageTimestamp),
            options: new NpgsqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntitiesWithStructComplexType_Redirect", Schema) });

         affectedRows.Should().Be(1);

         var columnSum = await AssertDbContext.Database
                                              .SqlQueryRaw<long>($"""SELECT "effective_received_at" * 1000 + "effective_message_timestamp" AS "Value" FROM "{Schema}"."TestEntitiesWithStructComplexType_Redirect" """)
                                              .SingleAsync();
         columnSum.Should().Be(100_200);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntitiesWithStructComplexType_Redirect" """);
      }
   }

   [Fact]
   public async Task Should_update_whole_nested_struct_ComplexType()
   {
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = _structComplexTypeId, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         var source = new TestEntityWithNestedStructComplexType { Id = _structComplexTypeId, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } };
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new NpgsqlTempTableBulkInsertOptions());

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
         await DropNestedStructComplexTypeTableAsync();
      }
   }

   [Fact]
   public async Task Should_update_nested_struct_ComplexType_member()
   {
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = _structComplexTypeId, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         var source = new TestEntityWithNestedStructComplexType { Id = _structComplexTypeId, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } };
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new NpgsqlTempTableBulkInsertOptions());

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
         await DropNestedStructComplexTypeTableAsync();
      }
   }

   private async Task ArrangeStructComplexTypeEntitiesAsync()
   {
      ArrangeDbContext.TestEntitiesWithStructComplexType.AddRange(
         new TestEntityWithStructComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = new TimestampPair(1, 2), Name = "Original" },
         new TestEntityWithStructComplexType { Id = _structComplexTypeId, MessageTimestamp = 11, EffectiveMessage = new TimestampPair(3, 4), Name = "Untouched" });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private static TempTableWithStructComplexType CreateStructComplexTypeSource()
   {
      return new TempTableWithStructComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = new TimestampPair(100, 200) };
   }

   private async Task AssertStructComplexTypeEntitiesAsync(TimestampPair expectedEffectiveMessage, string expectedName)
   {
      var loaded = await AssertDbContext.TestEntitiesWithStructComplexType.OrderBy(e => e.MessageTimestamp).ToListAsync();
      loaded.Should().BeEquivalentTo(
      [
         new TestEntityWithStructComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = expectedEffectiveMessage, Name = expectedName },
         new TestEntityWithStructComplexType { Id = _structComplexTypeId, MessageTimestamp = 11, EffectiveMessage = new TimestampPair(3, 4), Name = "Untouched" }
      ]);
   }

   private async Task ArrangeConvertedComplexTypeEntitiesAsync()
   {
      ArrangeDbContext.TestEntitiesWithConvertedComplexType.AddRange(
         new TestEntityWithConvertedComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2)) },
         new TestEntityWithConvertedComplexType { Id = _structComplexTypeId, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private static TempTableWithConvertedComplexType CreateConvertedComplexTypeSource()
   {
      return new TempTableWithConvertedComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)) };
   }

   private async Task AssertConvertedComplexTypeEntitiesAsync(ConvertedTimestampPair expectedEffectiveMessage)
   {
      var loaded = await AssertDbContext.TestEntitiesWithConvertedComplexType.OrderBy(e => e.MessageTimestamp).ToListAsync();
      loaded.Should().BeEquivalentTo(
      [
         new TestEntityWithConvertedComplexType { Id = _structComplexTypeId, MessageTimestamp = 10, EffectiveMessage = expectedEffectiveMessage },
         new TestEntityWithConvertedComplexType { Id = _structComplexTypeId, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) }
      ]);
   }

   private async Task CreateNestedStructComplexTypeTableAsync()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE IF NOT EXISTS "{Schema}"."{TestEntityWithNestedStructComplexType.TableName}"
         (
            "id" uuid PRIMARY KEY,
            "sequence" bigint NOT NULL,
            "received_at" bigint NOT NULL,
            "message_timestamp" bigint NOT NULL
         );
         """);
   }

   private async Task DropNestedStructComplexTypeTableAsync()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."{TestEntityWithNestedStructComplexType.TableName}" """);
   }

   [Fact]
   public async Task Should_throw_when_EF_Property_is_called_on_complex_property()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new NpgsqlTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => EF.Property<int>(e.Boundary, "Upper"), (e, f) => f.Boundary.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("EF.Property is supported only on the lambda parameter, not through a complex property, a navigation or another expression.*");
   }

   [Fact]
   public async Task Should_throw_when_whole_ComplexType_and_member_are_both_set()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new NpgsqlTempTableBulkInsertOptions());

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
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceRows, new NpgsqlTempTableBulkInsertOptions());

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

      var sql = ExecutedCommands.Last(c => c.Contains("UPDATE"));
      sql.Should().Contain("\"Tagged_Discriminator\" = ").And.NotContain("CASE");
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
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceA, sourceB], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.Tagged, (e, s) => s.Outer.Z > 0 ? s.Tagged : e.Tagged)
                           .Set(e => e.OptionalPair, (e, s) => s.Outer.Z > 0 ? s.OptionalPair : e.OptionalPair));

      affectedRows.Should().Be(2);
      var loadedA = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowA.Id);
      loadedA.Tagged.Should().BeEquivalentTo(sourceA.Tagged);
      loadedA.OptionalPair.Should().Be(sourceA.OptionalPair);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowB.Id)).Should().BeEquivalentTo(rowB);

      var sql = ExecutedCommands.Last(c => c.Contains("UPDATE"));
      sql.Should().Contain("\"Tagged_Discriminator\" = CASE")
         .And.Contain("\"Tagged_X\" = CASE")
         .And.Contain("\"OptionalPair_ReceivedAt\" = CASE");
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

      var sql = ExecutedCommands.Last(c => c.Contains("UPDATE"));
      sql.Should().NotContain("CASE").And.Contain("\"Tagged_Discriminator\" = ");
   }

   [Fact]
   public async Task Should_set_whole_struct_ComplexType_from_captured_nullable_value()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;
      await ArrangeStructComplexTypeEntitiesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

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
      ExecutedCommands.Should().NotContain(c => c.Contains("UPDATE"));
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
      ExecutedCommands.Should().NotContain(c => c.Contains("UPDATE"));
   }

   [Fact]
   public async Task Should_throw_for_member_of_nullable_struct_ComplexType()
   {
      var act = () => ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id, builder => builder.Set(e => e.OptionalPair!.Value.ReceivedAt, (e, s) => s.OptionalPair!.Value.ReceivedAt));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The member 'OptionalPair' of '*' is a nullable complex property.*");
      ExecutedCommands.Should().NotContain(c => c.Contains("UPDATE"));
   }

   [Fact]
   public async Task Should_throw_for_whole_complex_collection()
   {
      ConfigureModel = TestEntityWithComplexCollection.Configure;

      var act = () => ActDbContext.Set<TestEntityWithComplexCollection>().BulkUpdateAsync(ActDbContext.Set<TestEntityWithComplexCollection>(), e => e.Id, s => s.Id, builder => builder.Set(e => e.Boundaries, (e, s) => s.Boundaries));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The complex collection 'Boundaries' is not supported.*");
   }

   private class TestKey
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
   }

   [Fact]
   public async Task Should_update_no_rows_when_filter_short_circuits_to_false()
   {
      var id1 = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      var id2 = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      ArrangeDbContext.Add(new TestEntity { Id = id1, Name = "A", RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = id2, Name = "B", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([
                                                                            new TestEntity { Id = id1, RequiredName = "RequiredName", Count = 5 },
                                                                            new TestEntity { Id = id2, RequiredName = "RequiredName", Count = 5 }
                                                                         ], new NpgsqlTempTableBulkInsertOptions());
      TestEntity? captured = null;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count), filter: (e, f) => captured != null && e.Name == captured.Name);

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
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
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
      ExecutedCommands.Last().Should().Contain("IS NOT NULL").And.NotContain("<> NULL");
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
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
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
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
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
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == nullName.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withName.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_source_query_uses_collection_parameter()
   {
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
      ExecutedCommands.Last().Should().Contain("IS NOT NULL");
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
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceA, sourceB], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => s.OptionalBoundary ?? e.OptionalBoundary)
                           .Set(e => e.Tagged, (e, s) => s.Tagged ?? e.Tagged));

      affectedRows.Should().Be(2);
      ExecutedCommands.Last().Should().Contain("Tagged_Discriminator").And.Contain("IS NULL").And.NotContain("= NULL");
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
         ActDbContext.TestEntities.FromSqlRaw($"SELECT * FROM \"{Schema}\".\"TestEntities\" WHERE \"Name\" = {{0}}", name), e => e.Id, s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_When_collection_parameter_is_EF_Parameter()
   {
      var match = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-00000000000B"), RequiredName = "RequiredName" };
      var other = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-00000000000C"), RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var ids = new List<Guid> { match.Id };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities.Where(s => EF.Parameter(ids).Contains(s.Id)), e => e.Id, s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1));

      ExecutedCommands.Last().Should().Contain("ANY");
      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_override_table_When_value_and_filter_use_target_parameter()
   {
      var id = new Guid("8C000000-0000-0000-0000-00000000001E");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE "{Schema}"."TestEntities_QueryUpdateTarget" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
            INSERT INTO "{Schema}"."TestEntities_QueryUpdateTarget" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
            VALUES ('8C000000-0000-0000-0000-00000000001E', 'RequiredName', NULL, 100, NULL, NULL, NULL, 0, 0);
            """);

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            ActDbContext.TestEntities.Where(e => e.Id == id),
            e => e.Id,
            f => f.Id,
            builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count),
            filter: (e, f) => e.Count >= 100,
            options: new NpgsqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateTarget", Schema) });

         affectedRows.Should().Be(1);

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("\"TestEntities_QueryUpdateTarget\" AS", Exactly.Once());
         lastCommand.Should().Contain("\"TestEntities\" AS", Exactly.Once());

         var redirected = await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "Count" AS "Value" FROM "{Schema}"."TestEntities_QueryUpdateTarget" """).SingleAsync();
         redirected.Should().Be(101);
         (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryUpdateTarget" """);
      }
   }

   [Fact]
   public async Task Should_update_override_table_from_source_query_over_model_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, Name = "Source", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryUpdateRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         INSERT INTO "{Schema}"."TestEntities_QueryUpdateRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0);
         """);

      try
      {
         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            ActDbContext.TestEntities.Where(e => e.Id == id),
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count + 40),
            options: new NpgsqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateRedirect", Schema) });

         affectedRows.Should().Be(1);

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("\"TestEntities_QueryUpdateRedirect\"", Exactly.Once());
         lastCommand.Should().Contain("\"TestEntities\" AS", Exactly.Once());

         var redirected = await AssertDbContext.Database
                                               .SqlQueryRaw<string>($"""SELECT "Name" || ':' || "Count" AS "Value" FROM "{Schema}"."TestEntities_QueryUpdateRedirect" """)
                                               .SingleAsync();
         redirected.Should().Be("Source:41");

         var source = await AssertDbContext.TestEntities.SingleAsync();
         source.Name.Should().Be("Source");
         source.Count.Should().Be(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryUpdateRedirect" """);
      }
   }

   [Fact]
   public async Task Should_update_table_in_override_schema_from_source_query_over_model_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, Name = "Source", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      var archiveSchema = $"{Schema}_archive";

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
            CREATE SCHEMA "{archiveSchema}";
            CREATE TABLE "{archiveSchema}"."TestEntities" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
            INSERT INTO "{archiveSchema}"."TestEntities" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
            VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, NULL, NULL, NULL, 0, 0);
            """);

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            ActDbContext.TestEntities.Where(e => e.Id == id),
            e => e.Id,
            f => f.Id,
            builder => builder.Set(e => e.Count, (e, f) => f.Count + 40),
            options: new NpgsqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(archiveSchema) });

         affectedRows.Should().Be(1);

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain(archiveSchema, Exactly.Once());
         lastCommand.Should().Contain("\"TestEntities\" AS", Exactly.Twice());

         var archivedCount = await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "Count" AS "Value" FROM "{archiveSchema}"."TestEntities" """).SingleAsync();
         archivedCount.Should().Be(41);
         (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP SCHEMA IF EXISTS "{archiveSchema}" CASCADE""");
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
         ExecutedCommands.Last().Should().Contain(" IN (");

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
      ExecutedCommands.Last().Should().Contain("\"OptionalBoundary_Upper\" = NULL").And.Contain("\"Tagged_X\" = 8");

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
      ExecutedCommands.Last().Should().Contain("\"Count\" = 2");
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

      if (itemMatches)
      {
         ExecutedCommands.Last().Should().Contain("+ 100");
      }
      else
      {
         ExecutedCommands.Last().Should().Contain("\"Count\" = 0");
         ExecutedCommands.Last().Should().NotContain("+ 100");
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

   private sealed record CapturedItem(Guid Id, bool Active);
}
