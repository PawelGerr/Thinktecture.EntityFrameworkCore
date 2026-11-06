using System.Globalization;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// The rows are written with raw ADO commands (multi-row INSERT or MySqlBulkCopy), so the INSERT statements are not visible in ExecutedCommands.
// A temp table exists in the session of the connection that created it, so the structure and the content are read through the connection of ActDbContext.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertIntoTempTableAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("577BFD36-21BC-4F9E-97B4-367B8F29B730");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3");

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkInsertIntoTempTableAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_entities_into_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var columns = await MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name);
      columns.Select(c => c.Name).Should().Equal(nameof(CustomTempTable.Column1), nameof(CustomTempTable.Column2));
      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(2);

      var loadedEntities = await tempTable.Query.OrderBy(e => e.Column1).ToListAsync();
      loadedEntities.Should().BeEquivalentTo(entities, config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_entities_into_temp_table_with_pk()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, b => b.HasKey(e => e.Column1));

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };

      var options = new MySqlTempTableBulkInsertOptions();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(2);
      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().Equal(nameof(CustomTempTable.Column1));

      // The explicit values are kept although the key is an auto-increment column.
      var loadedEntities = await tempTable.Query.OrderBy(e => e.Column1).ToListAsync();
      loadedEntities.Should().BeEquivalentTo(entities, config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_create_pk_before_bulk_insert_and_reject_duplicate_keys()
   {
      // MySQL has no "create primary key after the bulk insert"; the primary key is part of CREATE TEMPORARY TABLE.
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, b => b.HasKey(e => e.Column1));

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 1, Column2 = "value2" }
                     };

      // 1062: duplicate entry for the primary key
      var exception = await SUT.Awaiting(sut => sut.BulkInsertIntoTempTableAsync(entities, new MySqlTempTableBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);

      // The temp table is dropped and the connection is released if the insert fails.
      ActDbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_insert_scalar_values_into_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new[] { 1, 2, 3 };

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(values, options, CancellationToken.None);

      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(3);
      (await tempTable.Query.OrderBy(v => v).ToListAsync()).Should().Equal(1, 2, 3);
   }

   [Fact]
   public async Task Should_dispose_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" }
                     };

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       DropTableOnDispose = true,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      string tempTableName;

      // A closed connection is reset by the pool, which drops the temp table on its own. Keep the connection open to observe the DROP of the library.
      await ActDbContext.Database.OpenConnectionAsync();

      try
      {
         await using (var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options))
         {
            tempTableName = tempTable.Name;
            (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTableName)).Should().BeTrue();
         }

         (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTableName)).Should().BeFalse();
      }
      finally
      {
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_insert_into_existing_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      // First batch
      var firstBatch = new List<CustomTempTable>
                       {
                          new() { Column1 = 1, Column2 = "value1" }
                       };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(firstBatch, options);

      // Second batch into existing temp table
      var secondBatch = new List<CustomTempTable>
                        {
                           new() { Column1 = 2, Column2 = "value2" }
                        };

      await SUT.BulkInsertIntoTempTableAsync(secondBatch, tempTable, (IBulkInsertOptions?)null, CancellationToken.None);

      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(2);
      (await tempTable.Query.OrderBy(e => e.Column1).Select(e => e.Column2).ToListAsync()).Should().Equal("value1", "value2");
   }

   [Fact]
   public async Task Should_insert_entityType_without_touching_real_table()
   {
      var realEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Real",
                          RequiredName = "RealRequiredName",
                          Count = 1
                       };
      ArrangeDbContext.TestEntities.Add(realEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42,
                          ConvertibleClass = new ConvertibleClass(43)
                       };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([tempEntity], new MySqlTempTableBulkInsertOptions());

      var tempEntities = await tempTable.Query.ToListAsync();
      tempEntities.Should().BeEquivalentTo([
                                              new TestEntity
                                              {
                                                 Id = _TEST_ENTITY_ID,
                                                 Name = "Name",
                                                 RequiredName = "RequiredName",
                                                 Count = 42,
                                                 ConvertibleClass = new ConvertibleClass(43)
                                              }
                                           ]);

      var realEntities = await AssertDbContext.TestEntities.ToListAsync();
      realEntities.Should().BeEquivalentTo([
                                              new TestEntity
                                              {
                                                 Id = _TEST_ENTITY_ID,
                                                 Name = "Real",
                                                 RequiredName = "RealRequiredName",
                                                 Count = 1
                                              }
                                           ]);
   }

   [Fact]
   public async Task Should_insert_entityType_without_required_fields_if_excluded()
   {
      // The temp table is created with the properties to insert only (MySQL has no 'UsePropertiesToInsertForTempTableCreation' switch).
      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID
                   };
      var entities = new List<TestEntity> { entity };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, new MySqlTempTableBulkInsertOptions
                                                                                   {
                                                                                      PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => e.Id)
                                                                                   });

      (await MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name)).Select(c => c.Name).Should().Equal(nameof(TestEntity.Id));

      var result = await tempTable.Query.Select(t => new { t.Id }).ToListAsync();
      result.Should().BeEquivalentTo([
                                        new { Id = _TEST_ENTITY_ID }
                                     ]);
   }

   [Fact]
   public async Task Should_throw_if_required_columns_of_an_existing_temp_table_are_not_inserted()
   {
      // The temp table has all columns of the entity. The columns that are not inserted are NOT NULL without a default value.
      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID
                   };
      await using var tempTable = await ActDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions());

      // 1364: field doesn't have a default value (strict mode)
      var exception = await ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync([entity], tempTable, e => e.Id))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1364);

      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(0);
   }

   [Fact]
   public async Task Should_return_disposable_query()
   {
      var tempTableQuery = await SUT.BulkInsertIntoTempTableAsync(Array.Empty<TestEntity>(), new MySqlTempTableBulkInsertOptions());
      var query = tempTableQuery.Query;
      tempTableQuery.Dispose();

      // The connection is closed and returned to the pool, i.e. the session of the temp table is gone.
      // 1146: table doesn't exist
      var exception = await query.Awaiting(q => q.ToListAsync())
                                 .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1146);

      tempTableQuery.Invoking(q => q.Query).Should().Throw<ObjectDisposedException>();
      tempTableQuery.Invoking(q => q.Name).Should().Throw<ObjectDisposedException>();
   }

   [Fact]
   public async Task Should_return_detached_entities_for_entities_with_a_primary_key()
   {
      var testEntity = new TestEntity
                       {
                          Id = _OTHER_TEST_ENTITY_ID,
                          RequiredName = "Name1"
                       };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([testEntity], new MySqlTempTableBulkInsertOptions());

      var entities = await tempTable.Query.ToListAsync();

      ActDbContext.Entry(entities[0]).State.Should().Be(EntityState.Detached);
   }

   [Fact]
   public async Task Should_not_mess_up_temp_tables_with_alternating_requests_without_disposing_previous_one()
   {
      var testEntity1 = new TestEntity
                        {
                           Id = _OTHER_TEST_ENTITY_ID,
                           RequiredName = "Name1"
                        };
      var testEntity2 = new TestEntity
                        {
                           Id = _OTHER_TEST_ENTITY_ID,
                           RequiredName = "Name2"
                        };

      var options = new MySqlTempTableBulkInsertOptions();

      await using var tempTable1_1 = await SUT.BulkInsertIntoTempTableAsync([testEntity1], options);
      await using var tempTable2_1 = await SUT.BulkInsertIntoTempTableAsync([testEntity2], options);
      await using var tempTable1_2 = await SUT.BulkInsertIntoTempTableAsync([testEntity1], options);
      await using var tempTable2_2 = await SUT.BulkInsertIntoTempTableAsync([testEntity2], options);

      tempTable1_1.Query.ToList().Should().BeEquivalentTo([testEntity1]);
      tempTable1_2.Query.ToList().Should().BeEquivalentTo([testEntity1]);
      tempTable2_1.Query.ToList().Should().BeEquivalentTo([testEntity2]);
      tempTable2_2.Query.ToList().Should().BeEquivalentTo([testEntity2]);
   }

   [Fact]
   public async Task Should_not_mess_up_temp_tables_with_alternating_requests_with_disposing_previous_one()
   {
      var testEntity1 = new TestEntity
                        {
                           Id = _OTHER_TEST_ENTITY_ID,
                           RequiredName = "Name1"
                        };
      var testEntity2 = new TestEntity
                        {
                           Id = _OTHER_TEST_ENTITY_ID,
                           RequiredName = "Name2"
                        };

      var options = new MySqlTempTableBulkInsertOptions();

      await using (var tempTable1 = await SUT.BulkInsertIntoTempTableAsync([testEntity1], options))
      await using (var tempTable2 = await SUT.BulkInsertIntoTempTableAsync([testEntity2], options))
      {
         tempTable1.Query.ToList().Should().BeEquivalentTo([testEntity1]);
         tempTable2.Query.ToList().Should().BeEquivalentTo([testEntity2]);
      }

      await using (var tempTable1 = await SUT.BulkInsertIntoTempTableAsync([testEntity1], options))
      await using (var tempTable2 = await SUT.BulkInsertIntoTempTableAsync([testEntity2], options))
      {
         tempTable1.Query.ToList().Should().BeEquivalentTo([testEntity1]);
         tempTable2.Query.ToList().Should().BeEquivalentTo([testEntity2]);
      }
   }

   [Fact]
   public async Task Should_use_different_tables_for_temp_tables_of_the_same_entity_that_are_alive_at_once()
   {
      var options = new MySqlTempTableBulkInsertOptions();

      await using var first = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "First" }], options);
      await using var second = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Second" }], options);

      first.Name.Should().Be("TestEntities_1");
      second.Name.Should().Be("TestEntities_2");

      // The name of a disposed temp table is used again.
      await first.DisposeAsync();
      await using var third = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Third" }], options);

      third.Name.Should().Be("TestEntities_1");
      (await third.Query.Select(e => e.RequiredName).SingleAsync()).Should().Be("Third");
      (await second.Query.Select(e => e.RequiredName).SingleAsync()).Should().Be("Second");
   }

   [Fact]
   public async Task Should_properly_join_real_table_with_temp_table()
   {
      var realEntity = new TestEntity
                       {
                          Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                          RequiredName = "Name1"
                       };
      ArrangeDbContext.Add(realEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntity
                       {
                          Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                          RequiredName = "Name"
                       };

      var options = new MySqlTempTableBulkInsertOptions();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([tempEntity], options);

      var entities = await tempTable.Query
                                    .Join(ActDbContext.TestEntities, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                    .ToListAsync();

      entities.Should().BeEquivalentTo([new { temp = tempEntity, real = realEntity }]);

      entities = await tempTable.Query
                                .Join(ActDbContext.TestEntities, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                .ToListAsync();

      entities.Should().BeEquivalentTo([new { temp = tempEntity, real = realEntity }]);
   }

   [Fact]
   public async Task Should_throw_if_entity_contains_inlined_owned_type()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = _OTHER_TEST_ENTITY_ID,
                          InlineEntity = null!
                       };
      var testEntities = new[] { testEntity };

      await SUT.Invoking(sut => sut.BulkInsertIntoTempTableAsync(testEntities, new MySqlTempTableBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_throw_for_entities_with_separated_owned_type()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _OTHER_TEST_ENTITY_ID,
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      await SUT.Invoking(sut => sut.BulkInsertIntoTempTableAsync([testEntity], new MySqlTempTableBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_entities_contain_separated_owned_type_although_it_is_not_inserted()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _OTHER_TEST_ENTITY_ID,
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      // The temp table is created with the properties to insert, which are validated against owned types as well.
      await ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync([testEntity], e => e.Id))
                        .Should().ThrowAsync<NotSupportedException>()
                        .WithMessage("The entity 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateOne' must not contain owned entities.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([testEntity], new MySqlTempTableBulkInsertOptions());

      var loadedEntities = await tempTable.Query.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_return_number_of_inserted_rows_for_entities()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      tempTable.NumberOfInsertedRows.Should().Be(2);
   }

   [Fact]
   public async Task Should_return_number_of_inserted_rows_for_values()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new[] { 1, 2, 3 };

      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(values, options, CancellationToken.None);

      tempTable.NumberOfInsertedRows.Should().Be(3);
   }

   // The tests below cover behavior specific to MySQL: column mapping, batching, MySqlBulkCopy, command timeout, cancellation, transactions.

   [Fact]
   public void Should_copy_settings_of_options_and_provide_them_to_table_creation_and_insert()
   {
      var propertiesToInsert = IEntityPropertiesProvider.Include<CustomTempTable>(e => e.Column1);
      var options = new MySqlTempTableBulkInsertOptions
                    {
                       TruncateTableIfExists = true,
                       DropTableOnDispose = false,
                       TableNameProvider = DefaultTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced,
                       PropertiesToInsert = propertiesToInsert,
                       BatchSize = 7,
                       CommandTimeout = TimeSpan.FromSeconds(5),
                       UseBulkCopy = true
                    };

      new MySqlTempTableBulkInsertOptions(options).Should().BeEquivalentTo(options);

      options.GetTempTableCreationOptions().Should().BeEquivalentTo(new TempTableCreationOptions
                                                                    {
                                                                       TruncateTableIfExists = true,
                                                                       DropTableOnDispose = false,
                                                                       TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced,
                                                                       PropertiesToInclude = propertiesToInsert
                                                                    });
      options.GetBulkInsertOptions().Should().BeEquivalentTo(new MySqlBulkInsertOptions
                                                             {
                                                                PropertiesToInsert = propertiesToInsert,
                                                                BatchSize = 7,
                                                                CommandTimeout = TimeSpan.FromSeconds(5),
                                                                UseBulkCopy = true
                                                             });
   }

   [Fact]
   public async Task Should_accept_options_of_other_providers()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };
      var options = new GenericTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       PropertiesToInsert = IEntityPropertiesProvider.Include<CustomTempTable>(e => e.Column1)
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      tempTable.NumberOfInsertedRows.Should().Be(2);
      (await MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name)).Select(c => c.Name).Should().Equal(nameof(CustomTempTable.Column1));
      (await tempTable.Query.OrderBy(e => e.Column1).Select(e => e.Column1).ToListAsync()).Should().Equal(1, 2);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_entities_with_different_column_names(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var entities = new[]
                     {
                        new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Name 1", Count = 1 },
                        new TestEntityWithDifferentColumnNames { Id = _OTHER_TEST_ENTITY_ID, Name = null, Count = 2 }
                     };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var columns = await MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name);
      columns.Select(c => c.Name).Should().BeEquivalentTo("entity_id", "display_name", "item_count");

      var loadedEntities = await tempTable.Query.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().BeEquivalentTo(entities, config => config.WithStrictOrdering());
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_entity_with_json_column(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var entities = new[]
                     {
                        new TestEntityWithJsonColumns { Id = _TEST_ENTITY_ID, JsonColumn = """{"a":1}""" },
                        new TestEntityWithJsonColumns { Id = _OTHER_TEST_ENTITY_ID, JsonColumn = null }
                     };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var columns = await MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name);
      columns.Single(c => c.Name == nameof(TestEntityWithJsonColumns.JsonColumn)).Type.Should().Be("json");

      var loadedEntities = await tempTable.Query.ToListAsync();
      loadedEntities.Single(e => e.Id == _TEST_ENTITY_ID).JsonColumn.Should().Contain("\"a\"");
      loadedEntities.Single(e => e.Id == _OTHER_TEST_ENTITY_ID).JsonColumn.Should().BeNull();
   }

   [Theory]
   [InlineData(1, null, 1)]
   [InlineData(499, null, 1)]
   [InlineData(500, null, 1)] // the default batch size
   [InlineData(501, null, 2)]
   [InlineData(1000, null, 2)]
   [InlineData(1001, null, 3)]
   [InlineData(10, 1, 10)]
   [InlineData(10, 3, 4)]
   [InlineData(10, 10, 1)]
   [InlineData(10, 100, 1)]
   public async Task Should_split_rows_into_statements_according_to_batch_size(int rowCount, int? batchSize, int expectedStatements)
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = Enumerable.Range(0, rowCount).Select(i => new CustomTempTable(i, $"value {i}")).ToList();
      var options = new MySqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       BatchSize = batchSize
                    };

      // The status counters are per session. The connection of ActDbContext stays open, so the executor and the status queries share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      try
      {
         var statementsBefore = await GetSessionStatusAsync("Com_insert");
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);
         var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

         tempTable.NumberOfInsertedRows.Should().Be(rowCount);
         statements.Should().Be(expectedStatements);

         var loadedValues = await tempTable.Query.OrderBy(e => e.Column1).Select(e => e.Column1).ToListAsync();
         loadedValues.Should().Equal(Enumerable.Range(0, rowCount));
      }
      finally
      {
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   public async Task Should_throw_and_drop_temp_table_if_batch_size_is_not_positive(int batchSize)
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new MySqlTempTableBulkInsertOptions { BatchSize = batchSize };

      await SUT.Awaiting(sut => sut.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value")], options))
               .Should().ThrowAsync<ArgumentOutOfRangeException>()
               .WithMessage("*batch size*greater than 0*");

      ActDbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);

      // The name of the failed temp table is available again.
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value")], new MySqlTempTableBulkInsertOptions());
      tempTable.Name.Should().EndWith("_1");
   }

   [Fact]
   public async Task Should_insert_entities_with_bulk_copy()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(true);
      options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;
      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = null },
                        new() { Column1 = 3, Column2 = "O'Brien \\N\r\n\t\"" }
                     };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      tempTable.NumberOfInsertedRows.Should().Be(3);
      (await tempTable.Query.OrderBy(e => e.Column1).ToListAsync()).Should().BeEquivalentTo(entities, config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_entityType_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var entityWithValues = new TestEntity
                             {
                                Id = _TEST_ENTITY_ID,
                                Name = "Name",
                                RequiredName = "RequiredName",
                                Count = 42,
                                NullableCount = 7,
                                ConvertibleClass = new ConvertibleClass(5),
                                PropertyWithBackingField = 3
                             };
      entityWithValues.SetPrivateField(9);
      var entityWithNulls = new TestEntity
                            {
                               Id = _OTHER_TEST_ENTITY_ID,
                               RequiredName = "RequiredName",
                               Count = 0
                            };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([entityWithValues, entityWithNulls], options);

      tempTable.NumberOfInsertedRows.Should().Be(2);

      var loadedEntities = await tempTable.Query.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Should().BeEquivalentTo(entityWithNulls);
      loadedEntities[1].Should().BeEquivalentTo(entityWithValues);
      loadedEntities[1].GetPrivateField().Should().Be(9);
   }

   [Fact]
   public async Task Should_insert_all_rows_with_single_load_data_statement_and_ignore_batch_size()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(true);
      options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;
      options.BatchSize = 1;
      var entities = Enumerable.Range(0, 1001).Select(i => new CustomTempTable(i, $"value {i}")).ToList();

      await ActDbContext.Database.OpenConnectionAsync();

      try
      {
         var insertsBefore = await GetSessionStatusAsync("Com_insert");
         var loadsBefore = await GetSessionStatusAsync("Com_load");
         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);
         var inserts = await GetSessionStatusAsync("Com_insert") - insertsBefore;
         var loads = await GetSessionStatusAsync("Com_load") - loadsBefore;

         tempTable.NumberOfInsertedRows.Should().Be(1001);
         inserts.Should().Be(0);
         loads.Should().Be(1);
         (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(1001);
      }
      finally
      {
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_insert_into_existing_temp_table_with_bulk_copy()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(true);
      options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None;

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1")], options);

      await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(2, "value2")], tempTable, options, CancellationToken.None);

      (await tempTable.Query.OrderBy(e => e.Column1).Select(e => e.Column2).ToListAsync()).Should().Equal("value1", "value2");
   }

   [Fact]
   public async Task Should_throw_and_drop_temp_table_if_bulk_copy_inserts_duplicate_keys()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>(false);

      var options = CreateOptions(true);

      // LOAD DATA LOCAL implies IGNORE, i.e. the duplicate row would be dropped silently if the executor wouldn't compare the number of rows.
      await SUT.Awaiting(sut => sut.BulkInsertValuesIntoTempTableAsync(new[] { 1, 1 }, options, CancellationToken.None))
               .Should().ThrowAsync<MySqlException>().WithMessage("*only 1 was inserted*");

      ActDbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
   }

   [Theory]
   [InlineData(false, 500)]
   [InlineData(false, 30_000)]
   [InlineData(true, 500)]
   [InlineData(true, 30_000)]
   public async Task Should_insert_entities_with_command_timeout(bool useBulkCopy, int timeoutInMilliseconds)
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(useBulkCopy);
      options.CommandTimeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1"), new CustomTempTable(2, "value2")], options);

      tempTable.NumberOfInsertedRows.Should().Be(2);
      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, tempTable.Name)).Should().Be(2);
   }

   [Fact]
   public async Task Should_throw_and_release_connection_and_name_when_cancellation_is_requested()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      await SUT.Awaiting(sut => sut.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1")], new MySqlTempTableBulkInsertOptions(), cancellationTokenSource.Token))
               .Should().ThrowAsync<OperationCanceledException>();

      ActDbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1")], new MySqlTempTableBulkInsertOptions());
      tempTable.Name.Should().EndWith("_1");
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_keep_rows_of_temp_table_if_the_transaction_of_the_caller_is_committed(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(useBulkCopy);
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1"), new CustomTempTable(2, "value2")], options);

      tempTable.NumberOfInsertedRows.Should().Be(2);
      (await tempTable.Query.CountAsync()).Should().Be(2);

      await transaction.CommitAsync();

      (await tempTable.Query.CountAsync()).Should().Be(2);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_remove_rows_but_not_the_temp_table_if_the_transaction_of_the_caller_is_rolled_back(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = CreateOptions(useBulkCopy);
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1"), new CustomTempTable(2, "value2")], options);

      (await tempTable.Query.CountAsync()).Should().Be(2);

      await transaction.RollbackAsync();

      // CREATE TEMPORARY TABLE is not part of the transaction, the rows (InnoDB) are.
      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTable.Name)).Should().BeTrue();
      (await tempTable.Query.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_commit_the_transaction_of_the_caller_when_temp_table_is_created_and_dropped()
   {
      // CREATE TEMPORARY TABLE and DROP TEMPORARY TABLE don't end a transaction implicitly (as DROP TABLE does).
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();
      ActDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Real" });
      await ActDbContext.SaveChangesAsync();

      await using (var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Temp" }], new MySqlTempTableBulkInsertOptions()))
      {
         (await tempTable.Query.CountAsync()).Should().Be(1);
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.RollbackAsync();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_inside_a_TransactionScope_because_xa_transactions_reject_temp_tables()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

      // 4091: temporary tables cannot be accessed inside XA transactions
      var exception = await SUT.Awaiting(sut => sut.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value1")], new MySqlTempTableBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(4091);
   }

   // MySqlBulkCopy uses LOAD DATA LOCAL INFILE, which the connection has to allow explicitly (the server is started with --local-infile=1).
   // This must happen before the first use of the connection of ActDbContext.
   private MySqlTempTableBulkInsertOptions CreateOptions(bool useBulkCopy)
   {
      if (useBulkCopy)
      {
         var connectionString = new MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = true };
         ActDbContext.Database.SetConnectionString(connectionString.ConnectionString);
      }

      return new MySqlTempTableBulkInsertOptions { UseBulkCopy = useBulkCopy };
   }

   private async Task<long> GetSessionStatusAsync(string name)
   {
      await using var command = ActDbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = $"SHOW SESSION STATUS LIKE '{name}'";
      await using var reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();

      return Int64.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
   }

   // Options that are not specific to MySQL, so the executor has to copy them into its own options type.
   private sealed class GenericTempTableBulkInsertOptions : ITempTableBulkInsertOptions
   {
      public bool TruncateTableIfExists { get; init; }
      public bool DropTableOnDispose { get; init; } = true;
      public ITempTableNameProvider? TableNameProvider { get; init; }
      public IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; init; }
      public IEntityPropertiesProvider? PropertiesToInsert { get; init; }
   }
}
