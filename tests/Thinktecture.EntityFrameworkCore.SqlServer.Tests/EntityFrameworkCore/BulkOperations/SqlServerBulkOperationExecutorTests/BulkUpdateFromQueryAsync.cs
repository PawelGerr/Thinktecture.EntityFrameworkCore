using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryAsync : IntegrationTestsBase
{
   private SqlServerBulkOperationExecutor SUT => field ??= ActDbContext.GetService<SqlServerBulkOperationExecutor>();

   public BulkUpdateFromQueryAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(nonMatchingEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name));

      var lastCommand = ExecutedCommands.Last();
      lastCommand.Should().Contain("UPDATE");
      lastCommand.Should().Contain("SET");
      lastCommand.Should().Contain("INNER JOIN");
      lastCommand.Should().Contain("ON");
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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
         CREATE TABLE [{Schema}].[TestEntities_QueryUpdateRedirect] (
            [Id] uniqueidentifier NOT NULL,
            [RequiredName] nvarchar(max) NOT NULL,
            [Name] nvarchar(max) NULL,
            [Count] int NOT NULL,
            [NullableCount] int NULL,
            [ConvertibleClass] int NULL,
            [ParentId] uniqueidentifier NULL,
            [PropertyWithBackingField] int NOT NULL,
            [_privateField] int NOT NULL,
            CONSTRAINT [PK_TestEntities_QueryUpdateRedirect] PRIMARY KEY ([Id])
         );
         INSERT INTO [{Schema}].[TestEntities_QueryUpdateRedirect] ([Id], [RequiredName], [Name], [Count], [NullableCount], [ConvertibleClass], [ParentId], [PropertyWithBackingField], [_privateField])
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

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            sourceQuery,
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count),
            options: new SqlServerBulkUpdateFromQueryOptions { TableName = "TestEntities_QueryUpdateRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>($"""SELECT [Name] FROM [{Schema}].[TestEntities_QueryUpdateRedirect] """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("UpdatedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS [{Schema}].[TestEntities_QueryUpdateRedirect] """);
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE [{Schema}].[TestEntities_QueryUpdateRedirect] (
            [Id] uniqueidentifier NOT NULL,
            [RequiredName] nvarchar(max) NOT NULL,
            [Name] nvarchar(max) NULL,
            [Count] int NOT NULL,
            [NullableCount] int NULL,
            [ConvertibleClass] int NULL,
            [ParentId] uniqueidentifier NULL,
            [PropertyWithBackingField] int NOT NULL,
            [_privateField] int NOT NULL,
            CONSTRAINT [PK_TestEntities_QueryUpdateRedirect] PRIMARY KEY ([Id])
         );
         INSERT INTO [{Schema}].[TestEntities_QueryUpdateRedirect] ([Id], [RequiredName], [Name], [Count], [NullableCount], [ConvertibleClass], [ParentId], [PropertyWithBackingField], [_privateField])
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

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
            sourceQuery,
            e => e.Id,
            f => f.Id,
            builder => builder
               .Set(e => e.Name, (e, f) => f.Name)
               .Set(e => e.Count, (e, f) => f.Count),
            options: new SqlServerBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryUpdateRedirect", Schema) });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>($"""SELECT [Name] FROM [{Schema}].[TestEntities_QueryUpdateRedirect] """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("UpdatedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS [{Schema}].[TestEntities_QueryUpdateRedirect] """);
      }
   }

   [Fact]
   public async Task Should_throw_if_no_set_entries()
   {
      var updatedEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count > 0);

      var lastCommand = ExecutedCommands.Last();
      lastCommand.Should().Contain("INNER JOIN");
      lastCommand.Should().Contain("ON");
      lastCommand.Should().Contain("WHERE");
      lastCommand.Should().Contain("[Count]");
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         sourceQuery,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => DateTime.Now.Year));

      affectedRows.Should().Be(1);

      // DateTime.Now must not be evaluated to a constant at translation time but be translated server-side.
      ExecutedCommands.Last().Should().Contain("GETDATE()");

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqlServerTempTableBulkInsertOptions());

      await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => f.Count));

      var lastCommand = ExecutedCommands.Last();
      // Source columns should use mapped names
      lastCommand.Should().Contain("[entity_id]");
      lastCommand.Should().Contain("[display_name]");
      lastCommand.Should().Contain("[item_count]");
      // Target columns should use unmapped (property) names
      lastCommand.Should().Contain("[Name]");
      lastCommand.Should().Contain("[Count]");
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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(updatedEntities, new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqlServerTempTableBulkInsertOptions());

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

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqlServerTempTableBulkInsertOptions());

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
   public async Task Should_throw_when_EF_Property_is_called_on_complex_property()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqlServerTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => EF.Property<int>(e.Boundary, "Upper"), (e, f) => f.Boundary.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("EF.Property is supported only on the lambda parameter, not through a complex property, a navigation or another expression.*");
   }

   [Fact]
   public async Task Should_throw_when_whole_ComplexType_and_member_are_both_set()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqlServerTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(tempTable.Query, e => e.Id, f => f.Id, builder => builder.Set(e => e.Boundary, (e, f) => f.Boundary).Set(e => e.Boundary.Upper, (e, f) => 42));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The target column 'Boundary_Upper' is set more than once.*");
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_from_captured_variable()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ArrangeDbContext.Add(new TestEntityWithComplexType(id, new BoundaryValueObject(1, 2)));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqlServerTempTableBulkInsertOptions());

      var value = new BoundaryValueObject(7, 8);

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Boundary, (e, f) => value));

      affectedRows.Should().Be(1);
      var sql = ExecutedCommands.Last();
      sql.Should().Contain("[Boundary_Upper] = 7");
      sql.Should().Contain("[Boundary_Lower] = 8");

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(7);
      loaded.Boundary.Lower.Should().Be(8);
   }

   [Fact]
   public async Task Should_update_whole_nested_struct_ComplexType()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         var source = new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } };
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new SqlServerTempTableBulkInsertOptions());

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
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      await CreateNestedStructComplexTypeTableAsync();

      try
      {
         ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
         await ArrangeDbContext.SaveChangesAsync();

         var source = new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } };
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new SqlServerTempTableBulkInsertOptions());

         var affectedRows = await ActDbContext.Set<TestEntityWithNestedStructComplexType>().BulkUpdateAsync(
            tempTable.Query,
            e => e.Id,
            s => s.Id,
            builder => builder.Set(e => e.Envelope.Key.ReceivedAt, (e, s) => s.Envelope.Key.ReceivedAt));

         affectedRows.Should().Be(1);

         var loaded = await AssertDbContext.Set<TestEntityWithNestedStructComplexType>().SingleAsync();
         loaded.Envelope.Should().Be(new MessageEnvelope { Key = new TimestampPair(100, 2), Sequence = 3 });
      }
      finally
      {
         await DropNestedStructComplexTypeTableAsync();
      }
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_with_value_converted_members_from_captured_variable()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ConfigureModel = builder =>
                       {
                          TestEntityWithConvertedComplexType.Configure(builder);
                          TempTableWithConvertedComplexType.Configure(builder);
                       };

      // The SQL Server context has no DbSet for this entity, so EF uses the CLR type name as the table name.
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE [{Schema}].[TestEntityWithConvertedComplexType] (
            [id] uniqueidentifier NOT NULL,
            [message_timestamp] bigint NOT NULL,
            [effective_received_at] bigint NOT NULL,
            [effective_message_timestamp] bigint NOT NULL,
            PRIMARY KEY ([id], [message_timestamp])
         );
         """);

      try
      {
         ArrangeDbContext.Set<TestEntityWithConvertedComplexType>().AddRange(
            new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2)) },
            new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) });
         await ArrangeDbContext.SaveChangesAsync();

         var source = new TempTableWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)) };
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new SqlServerTempTableBulkInsertOptions());

         var value = new ConvertedTimestampPair(new TimestampValue(7), new TimestampValue(8));

         var affectedRows = await ActDbContext.Set<TestEntityWithConvertedComplexType>().BulkUpdateAsync(
            tempTable.Query,
            e => new { e.Id, e.MessageTimestamp },
            s => new { s.Id, s.MessageTimestamp },
            builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

         affectedRows.Should().Be(1);
         var sql = ExecutedCommands.Last();
         sql.Should().Contain("[effective_received_at] = CAST(7 AS bigint)");
         sql.Should().Contain("[effective_message_timestamp] = CAST(8 AS bigint)");

         var loaded = await AssertDbContext.Set<TestEntityWithConvertedComplexType>().OrderBy(e => e.MessageTimestamp).ToListAsync();
         loaded.Should().BeEquivalentTo(
         [
            new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = value },
            new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) }
         ]);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS [{Schema}].[TestEntityWithConvertedComplexType]""");
      }
   }

   private async Task CreateNestedStructComplexTypeTableAsync()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE [{Schema}].[{TestEntityWithNestedStructComplexType.TableName}] (
            [id] uniqueidentifier NOT NULL PRIMARY KEY,
            [sequence] bigint NOT NULL,
            [received_at] bigint NOT NULL,
            [message_timestamp] bigint NOT NULL
         );
         """);
   }

   private async Task DropNestedStructComplexTypeTableAsync()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS [{Schema}].[{TestEntityWithNestedStructComplexType.TableName}]""");
   }

   private class TestKey
   {
      public Guid Id { get; set; }
      public string? Name { get; set; }
   }
}
