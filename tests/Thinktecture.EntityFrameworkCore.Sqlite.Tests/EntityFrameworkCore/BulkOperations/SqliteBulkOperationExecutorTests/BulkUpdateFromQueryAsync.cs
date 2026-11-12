using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryAsync : IntegrationTestsBase
{
   private SqliteBulkOperationExecutor SUT => field ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateFromQueryAsync(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
      : base(testOutputHelper, providerFactoryFixture)
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_update_entities_in_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryUpdateRedirect" AS SELECT * FROM "TestEntities" WHERE 0;
         INSERT INTO "TestEntities_QueryUpdateRedirect" ("Id", "RequiredName", "Name", "Count", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, 0, 0);
         """);

      try
      {
         var updatedEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName", Name = "UpdatedName", Count = 99 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            tempTable.Query,
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count),
            options: new SqliteBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateRedirect") });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryUpdateRedirect" """)
                                                   .ToListAsync();
         redirectedNames.Should().BeEquivalentTo(["UpdatedName"]);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryUpdateRedirect" """);
      }
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(nonMatchingEntities, new SqliteTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_from_regular_dbset_query()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity_1);
      await ArrangeDbContext.SaveChangesAsync();

      var entity_2_id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      var entity_2 = new TestEntity { Id = entity_2_id, Name = "NameToUse", RequiredName = "RequiredName", Count = 99 };
      ArrangeDbContext.Add(entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      // Use entity_2 as source to update entity_2's own Count via a projected anonymous type
      var sourceQuery = ActDbContext.Set<TestEntity>().Where(e => e.Id == entity_2_id)
                                    .Select(e => new { Identifier = e.Id, Number = e.Count });

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Identifier,
         builder => builder.Set(e => e.Count, (e, f) => f.Number));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity_2_id);
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_throw_if_no_set_entries()
   {
      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

   [Fact]
   public async Task Should_update_with_captured_null_variable()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

      string? name = null;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => name));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().BeNull();
   }

   public static string StaticName => "StaticValue";
   private static readonly ConvertibleClass _staticConvertibleClass = new(42);

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_update_with_static_member_value()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_translate_datetime_now_server_side_instead_of_evaluating_it()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => DateTime.Now.Year));

      affectedRows.Should().Be(1);

      // DateTime.Now must not be evaluated to a constant at translation time but be translated server-side.
      ExecutedCommands.Last().Should().Contain("strftime");

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_update_with_string_to_upper()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "updated", RequiredName = "RequiredName", Count = 0 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqliteTempTableBulkInsertOptions());

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
   public async Task Should_update_ComplexType_members_from_temp_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary.Upper, (e, f) => f.Boundary.Upper));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(2);
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_from_temp_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary, (e, f) => f.Boundary));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(20);
   }

   [Fact]
   public async Task Should_throw_when_target_selector_accesses_member_of_non_complex_property()
   {
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.RequiredName.Length, (e, f) => 1));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The member 'RequiredName' is not a complex property of 'TestEntity'. Only simple property access expressions (e => e.Property), member access through complex properties (e => e.ComplexProperty.Property) or EF.Property calls on the lambda parameter are supported.");
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_when_selector_is_converted_to_object()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set<object>(e => e.Boundary, (e, f) => f.Boundary));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(20);
   }

   [Fact]
   public async Task Should_throw_when_target_selector_is_not_a_member_chain()
   {
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count + 1, (e, f) => 1));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("Only simple property access expressions (e => e.Property), member access through complex properties (e => e.ComplexProperty.Property) or EF.Property calls on the lambda parameter are supported.*");
   }

   [Fact]
   public async Task Should_throw_when_EF_Property_is_called_on_complex_property()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => EF.Property<int>(e.Boundary, "Upper"), (e, f) => f.Boundary.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("EF.Property is supported only on the lambda parameter, not through a complex property, a navigation or another expression.*");
   }

   [Fact]
   public async Task Should_throw_when_whole_ComplexType_and_member_are_both_set()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Boundary, (e, f) => f.Boundary).Set(e => e.Boundary.Upper, (e, f) => 42));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The target column 'Boundary_Upper' is set more than once.*");
   }

   [Fact]
   public async Task Should_update_via_EF_Property_on_converted_parameter()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName", Count = 5 }], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => EF.Property<int>((object)e, nameof(TestEntity.Count)), (e, f) => f.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(5);
   }

   [Fact]
   public async Task Should_throw_when_captured_whole_ComplexType_value_is_null()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());
      BoundaryValueObject? value = null;

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Boundary, (e, f) => value!));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The member 'Lower' of 'BoundaryValueObject' cannot be read because the captured value is null.*");
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_from_captured_variable()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var value = new BoundaryValueObject(7, 8);

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary, (e, f) => value));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(7);
      loaded.Boundary.Lower.Should().Be(8);
   }

   [Fact]
   public async Task Should_filter_using_ComplexType_members()
   {
      var id1 = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      var id2 = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id1, new BoundaryValueObject(1, 0)));
      ArrangeDbContext.Add(new TestEntityWithComplexType(id2, new BoundaryValueObject(50, 0)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(
                                     [
                                        new TestEntityWithComplexType(id1, new BoundaryValueObject(10, 20)),
                                        new TestEntityWithComplexType(id2, new BoundaryValueObject(10, 20))
                                     ],
                                     new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary.Lower, (e, f) => f.Boundary.Lower),
         filter: (e, f) => e.Boundary.Upper < f.Boundary.Upper);

      affectedRows.Should().Be(1);

      var loaded1 = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync(e => e.Id == id1);
      loaded1.Boundary.Lower.Should().Be(20);
      loaded1.Boundary.Upper.Should().Be(1);

      var loaded2 = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync(e => e.Id == id2);
      loaded2.Boundary.Lower.Should().Be(0);
      loaded2.Boundary.Upper.Should().Be(50);
   }

   [Fact]
   public async Task Should_throw_when_captured_object_in_filter_is_null()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName", Count = 5 }], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = null;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count), filter: (e, f) => e.Name == captured!.Name);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The member 'Name' of 'TestEntity' cannot be read because the captured value is null.*");
   }

   [Theory]
   [InlineData(null, 2)]
   [InlineData(5, 1)]
   public async Task Should_update_with_HasValue_of_captured_nullable(int? nullableInt, int expectedCount)
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => nullableInt.HasValue ? 1 : 2));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(expectedCount);
   }

   [Theory]
   [InlineData(true, 0)]
   [InlineData(false, 7)]
   public async Task Should_update_with_null_guarded_captured_object(bool capturedIsNull, int expectedCount)
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = capturedIsNull ? null : new TestEntity { Count = 7 };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => captured == null ? 0 : captured.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(expectedCount);
   }

   [Theory]
   [InlineData(true, 2)]
   [InlineData(false, 1)]
   public async Task Should_filter_with_null_guarded_captured_object(bool capturedIsNull, int expectedAffectedRows)
   {
      var id1 = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      var id2 = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      ArrangeDbContext.Add(new TestEntity { Id = id1, Name = "Original1", RequiredName = "RequiredName", Count = 1 });
      ArrangeDbContext.Add(new TestEntity { Id = id2, Name = "Original2", RequiredName = "RequiredName", Count = 10 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([
                                                                            new TestEntity { Id = id1, Name = "Updated", RequiredName = "RequiredName" },
                                                                            new TestEntity { Id = id2, Name = "Updated", RequiredName = "RequiredName" }
                                                                         ], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = capturedIsNull ? null : new TestEntity { Count = 5 };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count >= (captured == null ? 0 : captured.Count));

      affectedRows.Should().Be(expectedAffectedRows);

      if (!capturedIsNull)
         (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == id1)).Name.Should().Be("Original1");
   }

   [Theory]
   [InlineData(true, 10)]
   [InlineData(false, 15)]
   public async Task Should_evaluate_only_captured_part_of_mixed_expression(bool capturedIsNull, int expectedCount)
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName", Count = 0 });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName", Count = 10 }], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = capturedIsNull ? null : new TestEntity { Count = 5 };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count + (captured == null ? 0 : captured.Count)));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().NotContain("CASE");

      if (!capturedIsNull)
         ExecutedCommands.Last().Should().Contain("+ 5");

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(expectedCount);
   }

   [Fact]
   public async Task Should_update_with_method_call_on_captured_value()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, Name = "Original", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      var captured = new TestEntity { Name = "abc" };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Name, (e, f) => captured.Name!.ToUpper()));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().NotContain("upper(");
      (await AssertDbContext.TestEntities.SingleAsync()).Name.Should().Be("ABC");
   }

   [Fact]
   public async Task Should_throw_when_captured_nullable_value_is_null()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      int? captured = null;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => captured!.Value));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The member 'Value' of 'Nullable`1' cannot be read because the captured value is null.*");
   }

   [Fact]
   public async Task Should_throw_when_evaluation_of_captured_value_fails()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      var captured = new ThrowingValue();

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => captured.Count));

      (await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The expression '*' cannot be evaluated on the client:*"))
         .WithInnerException<InvalidOperationException>().WithMessage("Count is not available.");
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_from_new_expression()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary, (e, f) => new BoundaryValueObject(7, 8)));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(7);
      loaded.Boundary.Lower.Should().Be(8);
   }

   [Theory]
   [InlineData(true, 2)]
   [InlineData(false, 1)]
   public async Task Should_filter_with_null_guard_that_spans_a_column(bool capturedIsNull, int expectedAffectedRows)
   {
      var id1 = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      var id2 = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
      ArrangeDbContext.Add(new TestEntity { Id = id1, Name = "A", RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = id2, Name = "B", RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([
                                                                            new TestEntity { Id = id1, RequiredName = "RequiredName", Count = 5 },
                                                                            new TestEntity { Id = id2, RequiredName = "RequiredName", Count = 5 }
                                                                         ], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = capturedIsNull ? null : new TestEntity { Name = "A" };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count), filter: (e, f) => captured == null || e.Name == captured.Name);

      affectedRows.Should().Be(expectedAffectedRows);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == id1)).Count.Should().Be(5);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == id2)).Count.Should().Be(capturedIsNull ? 5 : 0);
   }

   [Theory]
   [InlineData(true, 5)]
   [InlineData(false, 12)]
   public async Task Should_update_with_conditional_that_spans_a_column(bool capturedIsNull, int expectedCount)
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName", Count = 5 }], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = capturedIsNull ? null : new TestEntity { Count = 7 };

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => captured == null ? f.Count : captured.Count + f.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(expectedCount);
   }

   [Fact]
   public async Task Should_throw_InvalidOperationException_when_evaluation_fails_with_other_exception_and_unread_null_instance()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = null;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => captured == null ? Fail() : captured.Count));

      (await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The expression '*' cannot be evaluated on the client:*"))
         .WithInnerException<FormatException>().WithMessage("Evaluation failed.");
   }

   [Fact]
   public async Task Should_not_execute_captured_query_during_translation()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = id, RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());
      var commandCountBefore = ExecutedCommands.Count;

      var act = () => ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => ActDbContext.Set<TestEntity>().Count()));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not translate value expression:*");
      ExecutedCommands.Skip(commandCountBefore).Should().NotContain(c => c.Contains("COUNT(*)"));
   }

   private static int Fail()
   {
      throw new FormatException("Evaluation failed.");
   }

   private class TestKey
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
   }

   private class ThrowingValue
   {
      public int Count => throw new InvalidOperationException("Count is not available.");
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
                                                                         ], new SqliteTempTableBulkInsertOptions());
      TestEntity? captured = null;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Count, (e, f) => f.Count), filter: (e, f) => captured != null && e.Name == captured.Name);

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.Select(e => e.Count).ToListAsync()).Should().Equal(0, 0);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_source_columns()
   {
      var toValues = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0001"));
      var toNulls = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0002"));
      ArrangeDbContext.AddRange(toValues, toNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceRows = new[] { TestEntityWithNullableComplexTypes.CreateWithValues(toValues.Id), TestEntityWithNullableComplexTypes.CreateWithNulls(toNulls.Id) };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceRows, new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => s.OptionalBoundary)
                           .Set(e => e.OptionalPair, (e, s) => s.OptionalPair)
                           .Set(e => e.Tagged, (e, s) => s.Tagged)
                           .Set(e => e.Outer, (e, s) => s.Outer)
                           .Set(e => e.OptionalOuter, (e, s) => s.OptionalOuter)
                           .Set(e => e.OptionalConverted, (e, s) => s.OptionalConverted));

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync()).Should().BeEquivalentTo(sourceRows);
   }

   [Fact]
   public async Task Should_set_whole_nullable_ComplexTypes_from_captured_values_and_nulls()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0003"));
      entity.OptionalBoundary = null;
      entity.Tagged = null;
      entity.OptionalConverted = null;
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var boundary = new BoundaryValueObject(2, 5);
      TimestampPair? pair = null;
      var tagged = new TaggedValues { X = 7, Text = "t" };
      var outer = new OuterWithOptionalInner { Z = 1, Inner = null };
      OptionalOuter? optionalOuter = null;
      ConvertedTimestampPair? converted = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2));

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary)
                           .Set(e => e.OptionalPair, (e, s) => pair)
                           .Set(e => e.Tagged, (e, s) => tagged)
                           .Set(e => e.Outer, (e, s) => outer)
                           .Set(e => e.OptionalOuter, (e, s) => optionalOuter)
                           .Set(e => e.OptionalConverted, (e, s) => converted));

      affectedRows.Should().Be(1);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      loaded.OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(2, 5));
      loaded.OptionalPair.Should().BeNull();
      loaded.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 7, Text = "t" });
      loaded.Outer.Should().BeEquivalentTo(new OuterWithOptionalInner { Z = 1, Inner = null });
      loaded.OptionalOuter.Should().BeNull();
      loaded.OptionalConverted.Should().Be(new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2)));
   }

   [Fact]
   public async Task Should_set_whole_ComplexTypes_from_client_conditional_with_non_null_captured_value()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0020"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      BoundaryValueObject? boundary = new BoundaryValueObject(2, 5);
      TimestampPair? pair = new TimestampPair(100, 200);
      TaggedValues? tagged = new TaggedValues { X = 7, Text = "t" };
      OuterWithOptionalInner? outer = new OuterWithOptionalInner { Z = 1, Inner = new InnerValue { V = 11 } };
      OptionalOuter? optionalOuter = new OptionalOuter { Y = 3, Inner = new InnerValue { V = 33 } };
      ConvertedTimestampPair? converted = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2));

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         ActDbContext.TestEntitiesWithNullableComplexTypes, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary == null ? s.OptionalBoundary : boundary)
                           .Set(e => e.OptionalPair, (e, s) => pair == null ? s.OptionalPair : pair)
                           .Set(e => e.Tagged, (e, s) => tagged == null ? s.Tagged : tagged)
                           .Set(e => e.Outer, (e, s) => outer == null ? s.Outer : outer)
                           .Set(e => e.OptionalOuter, (e, s) => optionalOuter == null ? s.OptionalOuter : optionalOuter)
                           .Set(e => e.OptionalConverted, (e, s) => converted == null ? s.OptionalConverted : converted));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).Should().BeEquivalentTo(TestEntityWithNullableComplexTypes.CreateWithValues(entity.Id));
   }

   [Fact]
   public async Task Should_set_whole_ComplexTypes_from_client_conditional_with_null_captured_value()
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0021"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceRow = TestEntityWithNullableComplexTypes.CreateWithValues(entity.Id);
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceRow], new SqliteTempTableBulkInsertOptions());

      BoundaryValueObject? boundary = null;
      TimestampPair? pair = null;
      TaggedValues? tagged = null;
      OuterWithOptionalInner? outer = null;
      OptionalOuter? optionalOuter = null;
      ConvertedTimestampPair? converted = null;

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary == null ? s.OptionalBoundary : boundary)
                           .Set(e => e.OptionalPair, (e, s) => pair == null ? s.OptionalPair : pair)
                           .Set(e => e.Tagged, (e, s) => tagged == null ? s.Tagged : tagged)
                           .Set(e => e.Outer, (e, s) => outer == null ? s.Outer : outer)
                           .Set(e => e.OptionalOuter, (e, s) => optionalOuter == null ? s.OptionalOuter : optionalOuter)
                           .Set(e => e.OptionalConverted, (e, s) => converted == null ? s.OptionalConverted : converted));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).Should().BeEquivalentTo(sourceRow);
   }

   [Fact]
   public async Task Should_set_whole_ComplexTypes_from_server_conditional()
   {
      var rowA = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0022"));
      var rowB = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0023"));
      ArrangeDbContext.AddRange(rowA, rowB);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceA = TestEntityWithNullableComplexTypes.CreateWithValues(rowA.Id);
      var sourceB = TestEntityWithNullableComplexTypes.CreateWithNulls(rowB.Id);
      sourceB.Outer.Z = 0;
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceA, sourceB], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.Tagged, (e, s) => s.Outer.Z > 0 ? s.Tagged : e.Tagged)
                           .Set(e => e.OptionalPair, (e, s) => s.Outer.Z > 0 ? s.OptionalPair : e.OptionalPair)
                           .Set(e => e.OptionalOuter, (e, s) => s.Outer.Z > 0 ? s.OptionalOuter : e.OptionalOuter));

      affectedRows.Should().Be(2);
      var loadedA = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowA.Id);
      loadedA.Tagged.Should().BeEquivalentTo(sourceA.Tagged);
      loadedA.OptionalPair.Should().Be(sourceA.OptionalPair);
      loadedA.OptionalOuter.Should().BeEquivalentTo(sourceA.OptionalOuter);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowB.Id)).Should().BeEquivalentTo(rowB);
   }

   [Fact]
   public async Task Should_set_whole_ComplexType_from_server_conditional_with_captured_branch()
   {
      var rowA = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0024"));
      var rowB = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0025"));
      ArrangeDbContext.AddRange(rowA, rowB);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceA = TestEntityWithNullableComplexTypes.CreateWithValues(rowA.Id);
      var sourceB = TestEntityWithNullableComplexTypes.CreateWithNulls(rowB.Id);
      sourceB.Outer.Z = 0;
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceA, sourceB], new SqliteTempTableBulkInsertOptions());

      var tagged = new TaggedValues { X = 8, Text = "c" };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.Tagged, (e, s) => s.Outer.Z > 0 ? tagged : s.Tagged));

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowA.Id)).Tagged.Should().BeEquivalentTo(new TaggedValues { X = 8, Text = "c" });
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == rowB.Id)).Tagged.Should().BeNull();
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_set_whole_nullable_ComplexType_from_coalesce(bool capturedHasValue)
   {
      var entity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("9A5C2E6D-3B8B-4C9F-8A68-AE5D9F4B0026"));
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceRow = TestEntityWithNullableComplexTypes.CreateWithValues(entity.Id);
      sourceRow.OptionalBoundary = new BoundaryValueObject(3, 9);
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceRow], new SqliteTempTableBulkInsertOptions());

      var boundary = capturedHasValue ? new BoundaryValueObject(2, 5) : null;

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkUpdateAsync(
         tempTable.Query, e => e.Id, s => s.Id,
         builder => builder.Set(e => e.OptionalBoundary, (e, s) => boundary ?? s.OptionalBoundary));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).OptionalBoundary
         .Should().BeEquivalentTo(capturedHasValue ? new BoundaryValueObject(2, 5) : new BoundaryValueObject(3, 9));
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
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceA, sourceB], new SqliteTempTableBulkInsertOptions());

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
         ActDbContext.TestEntities.FromSql($"SELECT * FROM \"TestEntities\" WHERE \"Name\" = {name}"), e => e.Id, s => s.Id,
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

      ExecutedCommands.Last().Should().Contain("json_each");
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

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryUpdateTarget" AS SELECT * FROM "TestEntities" WHERE 0;
         INSERT INTO "TestEntities_QueryUpdateTarget" ("Id", "RequiredName", "Name", "Count", "PropertyWithBackingField", "_privateField")
         VALUES ('8C000000-0000-0000-0000-00000000001E', 'RequiredName', NULL, 100, 0, 0);
         """);

      try
      {
         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            ActDbContext.TestEntities.Where(e => e.Id == id),
            e => e.Id,
            f => f.Id,
            builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count),
            filter: (e, f) => e.Count >= 100,
            options: new SqliteBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateTarget") });

         affectedRows.Should().Be(1);

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("\"TestEntities_QueryUpdateTarget\"", Exactly.Once());
         lastCommand.Should().Contain("\"TestEntities\" AS", Exactly.Once());

         var redirected = await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "Count" AS "Value" FROM "TestEntities_QueryUpdateTarget" """).SingleAsync();
         redirected.Should().Be(101);
         (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryUpdateTarget" """);
      }
   }

   [Fact]
   public async Task Should_update_override_table_from_source_query_over_model_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntity { Id = id, Name = "Source", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryUpdateRedirect" AS SELECT * FROM "TestEntities" WHERE 0;
         INSERT INTO "TestEntities_QueryUpdateRedirect" ("Id", "RequiredName", "Name", "Count", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', NULL, 0, 0, 0);
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
            options: new SqliteBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateRedirect") });

         affectedRows.Should().Be(1);

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("\"TestEntities_QueryUpdateRedirect\"", Exactly.Once());
         lastCommand.Should().Contain("\"TestEntities\" AS", Exactly.Once());

         var redirected = await AssertDbContext.Database
                                               .SqlQueryRaw<string>("""SELECT "Name" || ':' || "Count" AS "Value" FROM "TestEntities_QueryUpdateRedirect" """)
                                               .SingleAsync();
         redirected.Should().Be("Source:41");

         var source = await AssertDbContext.TestEntities.SingleAsync();
         source.Name.Should().Be("Source");
         source.Count.Should().Be(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryUpdateRedirect" """);
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
         ExecutedCommands.Last().Should().Contain(" IN (");

      if (arrayLength == 1)
         ExecutedCommands.Last().Should().NotContain(" IN (");

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
      ExecutedCommands.Last().Should().Contain(" IN ('x', 'y')");
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
      ExecutedCommands.Last().Should().Contain(" IN (1, 2)");
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
      ExecutedCommands.Last().Should().Contain("CASE");
      ExecutedCommands.Last().Should().Contain(" IN (");

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
      ExecutedCommands.Last().Should().Contain("\"Count\" = 2");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(2);
   }

   [Theory]
   [InlineData(true, 1)]
   [InlineData(false, 0)]
   public async Task Should_update_rows_When_filter_uses_captured_array_SequenceEqual(bool equal, int expectedAffectedRows)
   {
      var entity = new TestEntity { Id = new Guid("8B000000-0000-0000-0000-0000000000A9"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      int[] left = [1, 3];
      int[] right = equal ? [1, 3] : [1, 4];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => left.SequenceEqual(right) && s.Id == entity.Id);

      affectedRows.Should().Be(expectedAffectedRows);

      if (equal)
         ExecutedCommands.Last().Should().NotContain("1 = 0");
      else
         ExecutedCommands.Last().Should().Contain("1 = 0");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == entity.Id)).Count.Should().Be(expectedAffectedRows);
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
      ExecutedCommands.Last().Should().Contain("THEN 2").And.Contain("ELSE 1");

      var stored = await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "ConvertibleClass" AS "Value" FROM "TestEntities" ORDER BY "Count" """).ToListAsync();
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
      ExecutedCommands.Last().Should().Contain("THEN 11").And.Contain("ELSE 21").And.Contain("THEN 12").And.Contain("ELSE 22");

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.OrderBy(e => e.Id).ToListAsync();
      loaded[0].OptionalConverted.Should().Be(forValues);
      loaded[1].OptionalConverted.Should().Be(forNulls);
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
      ExecutedCommands.Last().Should().Contain(".\"NullableCount\" IS NULL").And.Contain(".\"NullableCount\" = 7");

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
      ExecutedCommands.Last().Should().Contain("1 = 0");
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_list_with_only_null_element()
   {
      var withNull = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000012"), RequiredName = "RequiredName", NullableCount = null };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000013"), RequiredName = "RequiredName", NullableCount = 8 };
      ArrangeDbContext.AddRange(withNull, other);
      await ArrangeDbContext.SaveChangesAsync();

      var counts = new List<int?> { null };

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => counts.Contains(s.NullableCount));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("\"NullableCount\" IS NULL").And.NotContain(" IN (");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == withNull.Id)).Count.Should().Be(1);
   }

   [Fact]
   public async Task Should_update_rows_When_filter_uses_captured_string_array_with_null_element()
   {
      var withNull = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000014"), RequiredName = "RequiredName", Name = null };
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000015"), RequiredName = "RequiredName", Name = "x" };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000016"), RequiredName = "RequiredName", Name = "y" };
      ArrangeDbContext.AddRange(withNull, match, other);
      await ArrangeDbContext.SaveChangesAsync();

      string?[] names = ["x", null];

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => names.Contains(s.Name));

      affectedRows.Should().Be(2);
      ExecutedCommands.Last().Should().Contain("\"Name\" IS NULL").And.Contain("\"Name\" = 'x'");
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   // Guard: passes without the rewrite in ClosureFlatteningVisitor.VisitMethodCall.
   [Fact]
   public async Task Should_throw_When_client_evaluated_Contains_uses_captured_null_collection()
   {
      int[]? ids = null;

      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => ids!.Contains(5) ? 1 : 2));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The expression '*' cannot be evaluated on the client: Value cannot be null.*");
   }

   // Guard: passes without the rewrite in ClosureFlatteningVisitor.VisitMethodCall.
   [Fact]
   public async Task Should_throw_When_filter_uses_Contains_of_captured_non_collection_object()
   {
      var range = new IntRange(1, 10);

      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => range.Contains(s.Count));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not translate filter expression:*");
   }

   [Fact]
   public async Task Should_enumerate_captured_collection_once_When_filter_uses_Contains()
   {
      var match = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-00000000001F"), RequiredName = "RequiredName", NullableCount = 7 };
      var other = new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000020"), RequiredName = "RequiredName", NullableCount = 8 };
      ArrangeDbContext.AddRange(match, other);
      await ArrangeDbContext.SaveChangesAsync();

      var enumerations = 0;
      IEnumerable<int?> counts = EnumerateOnce();

      IEnumerable<int?> EnumerateOnce()
      {
         if (++enumerations > 1)
            throw new InvalidOperationException("The captured collection was enumerated more than once.");

         yield return 7;
      }

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => counts.Contains(s.NullableCount));

      affectedRows.Should().Be(1);
      enumerations.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == match.Id)).Count.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == other.Id)).Count.Should().Be(0);
   }

   // Guard: a string is not a collection, so a captured null string keeps the translation of EF Core and does not become a false condition.
   [Fact]
   public async Task Should_keep_translation_of_string_Contains_When_captured_string_is_null()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("8C000000-0000-0000-0000-000000000021"), RequiredName = "RequiredName", Name = "x" });
      await ArrangeDbContext.SaveChangesAsync();

      string? nullStr = null;

      var affectedRows = await ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => nullStr!.Contains(s.Name!));

      affectedRows.Should().Be(0);
      ExecutedCommands.Last().Should().NotContain("1 = 0");
   }

   // Guard: EF Core treats only IList and ICollection<T> as collections, so the own Contains method of any other IEnumerable is not replaced with IN (...).
   [Fact]
   public async Task Should_throw_When_filter_uses_own_Contains_of_captured_enumerable_that_is_not_a_collection()
   {
      var sequence = new IntSequence();

      var act = () => ActDbContext.TestEntities.BulkUpdateAsync(
         ActDbContext.TestEntities,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Count, (e, s) => 1),
         filter: (e, s) => sequence.Contains(s.Count));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not translate filter expression:*");
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
      ExecutedCommands.Last().Should().Contain(".\"NullableCount\" IS NULL");
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

   private sealed class IntRange(int lower, int upper)
   {
      public bool Contains(int value) => value >= lower && value <= upper;
   }

   private sealed class IntSequence : IEnumerable<int>
   {
      public bool Contains(int value) => value % 2 == 0;

      public IEnumerator<int> GetEnumerator() => Enumerable.Range(1, 3).GetEnumerator();

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
   }

   private sealed record CapturedItem(Guid Id, bool Active);
}
