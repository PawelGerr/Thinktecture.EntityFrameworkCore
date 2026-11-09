using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertIntoTempTableAsync : IntegrationTestsBase
{
   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public BulkInsertIntoTempTableAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
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

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var columns = await ActDbContext.GetTempTableColumns(tempTable.Name).ToListAsync();
      columns.Should().HaveCount(2);

      var count = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT COUNT(*)::int AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      count.Should().Be(2);
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

      var options = new NpgsqlTempTableBulkInsertOptions();

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var count = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT COUNT(*)::int AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      count.Should().Be(2);

      var constraints = await ActDbContext.GetTempTableConstraints(tempTable.Name).ToListAsync();
      constraints.Should().Contain(c => c.ConstraintType == "PRIMARY KEY");
   }

   [Fact]
   public async Task Should_create_pk_after_bulk_insert_by_default()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, b => b.HasKey(e => e.Column1));

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       MomentOfPrimaryKeyCreation = MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var constraints = await ActDbContext.GetTempTableConstraints(tempTable.Name).ToListAsync();
      constraints.Should().Contain(c => c.ConstraintType == "PRIMARY KEY");
   }

   [Fact]
   public async Task Should_create_pk_before_bulk_insert()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, b => b.HasKey(e => e.Column1));

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" },
                        new() { Column1 = 2, Column2 = "value2" }
                     };

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       MomentOfPrimaryKeyCreation = MomentOfNpgsqlPrimaryKeyCreation.BeforeBulkInsert
                    };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options);

      var constraints = await ActDbContext.GetTempTableConstraints(tempTable.Name).ToListAsync();
      constraints.Should().Contain(c => c.ConstraintType == "PRIMARY KEY");

      var count = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT COUNT(*)::int AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      count.Should().Be(2);
   }

   [Fact]
   public async Task Should_insert_scalar_values_into_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new[] { 1, 2, 3 };

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(values, options, CancellationToken.None);

      var count = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT COUNT(*)::int AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      count.Should().Be(3);
   }

   [Fact]
   public async Task Should_dispose_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var entities = new List<CustomTempTable>
                     {
                        new() { Column1 = 1, Column2 = "value1" }
                     };

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       DropTableOnDispose = true,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      string tempTableName;

      await using (var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, options))
      {
         tempTableName = tempTable.Name;
      }

      var columns = await ActDbContext.GetTempTableColumns(tempTableName).ToListAsync();
      columns.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_insert_into_existing_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableBulkInsertOptions
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

      var count = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT COUNT(*)::int AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      count.Should().Be(2);
   }

   [Fact]
   public async Task Should_insert_entityType_without_touching_real_table()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42,
                      ConvertibleClass = new ConvertibleClass(43)
                   };
      ArrangeDbContext.TestEntities.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var entities = new List<TestEntity> { entity };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, new NpgsqlTempTableBulkInsertOptions());

      var tempEntities = await tempTable.Query.ToListAsync();
      tempEntities.Should().BeEquivalentTo([
         new TestEntity
                                              {
                                                 Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                                                 Name = "Name",
                                                 RequiredName = "RequiredName",
                                                 Count = 42,
                                                 ConvertibleClass = new ConvertibleClass(43)
                                              },
      ]);
   }

   [Fact]
   public async Task Should_insert_entityType_without_required_fields_if_excluded_and_with_UsePropertiesToInsertForTempTableCreation()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730")
                   };
      var entities = new List<TestEntity> { entity };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(entities, new NpgsqlTempTableBulkInsertOptions
                                                                                    {
                                                                                       PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => e.Id),
                                                                                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                                                                                    });

      var result = await tempTable.Query.Select(t => new { t.Id }).ToListAsync();
      result.Should().BeEquivalentTo([
         new { Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730") },
      ]);
   }

   [Fact]
   public async Task Should_insert_entityType_without_required_fields_if_excluded_and_without_UsePropertiesToInsertForTempTableCreation()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730")
                   };
      var entities = new List<TestEntity> { entity };

      await SUT.Awaiting(sut => sut.BulkInsertIntoTempTableAsync(entities, new NpgsqlTempTableBulkInsertOptions
                                                                     {
                                                                        PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => e.Id),
                                                                        Advanced = { UsePropertiesToInsertForTempTableCreation = false }
                                                                     }))
         .Should().ThrowAsync<PostgresException>().WithMessage("""
         23502: null value in column "Count" of relation "TestEntities_1" violates not-null constraint

         DETAIL: Detail redacted as it may contain sensitive data. Specify 'Include Error Detail' in the connection string to include this information.
         """);

   }

   [Fact]
   public async Task Should_return_detached_entities_for_entities_with_a_primary_key()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          RequiredName = "Name1"
                       };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([testEntity], new NpgsqlTempTableBulkInsertOptions());

      var entities = await tempTable.Query.ToListAsync();

      ActDbContext.Entry(entities[0]).State.Should().Be(EntityState.Detached);
   }

   [Fact]
   public async Task Should_not_mess_up_temp_tables_with_alternating_requests_without_disposing_previous_one()
   {
      var testEntity1 = new TestEntity
                        {
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name1"
                        };
      var testEntity2 = new TestEntity
                        {
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name2"
                        };

      var options = new NpgsqlTempTableBulkInsertOptions();

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
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name1"
                        };
      var testEntity2 = new TestEntity
                        {
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name2"
                        };

      var options = new NpgsqlTempTableBulkInsertOptions();

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

      var options = new NpgsqlTempTableBulkInsertOptions();

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
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = null!
                       };
      var testEntities = new[] { testEntity };

      await SUT.Invoking(sut => sut.BulkInsertIntoTempTableAsync(testEntities, new NpgsqlTempTableBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_throw_for_entities_with_separated_owned_type()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      await SUT.Invoking(sut => sut.BulkInsertIntoTempTableAsync([testEntity], new NpgsqlTempTableBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([testEntity], new NpgsqlTempTableBulkInsertOptions());

      var loadedEntities = await tempTable.Query.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_temp_table_entity_with_struct_ComplexType()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;

      var entity = new TempTableWithStructComplexType
                   {
                      Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                      MessageTimestamp = 10,
                      EffectiveMessage = new TimestampPair(100, 200)
                   };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([entity], new NpgsqlTempTableBulkInsertOptions());

      var loadedEntities = await tempTable.Query.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity]);

      var columns = await ActDbContext.GetTempTableColumns(tempTable.Name).ToListAsync();
      columns.Select(c => c.ColumnName).Should().BeEquivalentTo("id", "message_timestamp", "effective_received_at", "effective_message_timestamp");

      foreach (var column in columns.Where(c => c.ColumnName!.StartsWith("effective_", StringComparison.Ordinal)))
      {
         column.IsNullable.Should().BeFalse();
         column.DataType.Should().Be("bigint");
      }
   }

   [Fact]
   public async Task Should_project_struct_ComplexType_members_from_temp_table()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;

      var entity = new TempTableWithStructComplexType
                   {
                      Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                      MessageTimestamp = 10,
                      EffectiveMessage = new TimestampPair(100, 200)
                   };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([entity], new NpgsqlTempTableBulkInsertOptions());

      var projected = await tempTable.Query.Select(e => new { e.Id, e.EffectiveMessage.ReceivedAt }).ToListAsync();
      projected.Should().BeEquivalentTo([new { entity.Id, ReceivedAt = 100L }]);
   }

   [Fact]
   public async Task Should_insert_temp_table_entity_with_value_converted_members_of_struct_ComplexType()
   {
      ConfigureModel = TempTableWithConvertedComplexType.Configure;

      var entity = new TempTableWithConvertedComplexType
                   {
                      Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                      MessageTimestamp = 10,
                      EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200))
                   };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([entity], new NpgsqlTempTableBulkInsertOptions());

      var loadedEntities = await tempTable.Query.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity]);

      var columns = await ActDbContext.GetTempTableColumns(tempTable.Name).ToListAsync();

      foreach (var column in columns.Where(c => c.ColumnName!.StartsWith("effective_", StringComparison.Ordinal)))
      {
         column.IsNullable.Should().BeFalse();
         column.DataType.Should().Be("bigint");
      }
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

      var options = new NpgsqlTempTableBulkInsertOptions
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

      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                    };

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(values, options, CancellationToken.None);

      tempTable.NumberOfInsertedRows.Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_and_read_back_nullable_ComplexTypes_from_temp_table()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("7F4B1D5C-2A7A-4B8E-9F57-9D4C8E3A0001"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("7F4B1D5C-2A7A-4B8E-9F57-9D4C8E3A0002"));

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([withValues, withNulls], new NpgsqlTempTableBulkInsertOptions());

      var loaded = await tempTable.Query.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls]);
   }

   [Fact]
   public async Task Should_insert_struct_with_value_converter()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Timestamp>(b => b.Property(t => t.Column1).HasConversion<TimestampConverter>());

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Timestamp> { new(1), new(2) }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([new Timestamp(1), new Timestamp(2)]);
   }

   [Fact]
   public async Task Should_insert_enum_with_string_conversion_as_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Color>(b => b.Property(t => t.Column1).HasConversion<string>());

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Color> { Color.Red }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var storedValue = await ActDbContext.Database.SqlQueryRaw<string>($"""SELECT "Column1" AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      storedValue.Should().Be("Red");

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([Color.Red]);
   }

   [Fact]
   public async Task Should_insert_nullable_struct_with_value_converter_including_null()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Timestamp?>(b => b.Property(t => t.Column1).HasConversion<TimestampConverter>());

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Timestamp?> { new Timestamp(1), null }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([new Timestamp(1), (Timestamp?)null]);
   }

   [Fact]
   public async Task Should_insert_struct_with_value_converter_into_existing_temp_table()
   {
      // Arrange
      ConfigureModel = builder => builder.ConfigureTempTable<Timestamp>(b => b.Property(t => t.Column1).HasConversion<TimestampConverter>());

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Timestamp>(), new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      // Act
      await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<Timestamp> { new(1), new(2) }, tempTable);

      // Assert
      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([new Timestamp(1), new Timestamp(2)]);
   }

   [Fact]
   public async Task Should_insert_enum_without_explicit_conversion()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Color>();

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Color> { Color.Red }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([Color.Red]);
   }

   [Fact]
   public async Task Should_insert_number_based_value_object()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Quantity>(b => b.Property(t => t.Column1).HasConversion(v => v.Value, v => new Quantity(v)));

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Quantity> { new(3) }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var storedValue = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT "Column1" AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      storedValue.Should().Be(3);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([new Quantity(3)]);
   }

   [Fact]
   public async Task Should_insert_DateTime_based_value_object()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Moment>(b => b.Property(t => t.Column1).HasConversion(v => v.Value, v => new Moment(v)));

      var moment = new Moment(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<Moment> { moment }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([moment]);
   }

   [Fact]
   public async Task Should_apply_value_changing_converter_of_number()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>(b => b.Property(t => t.Column1).HasConversion(v => v * 100, v => v / 100));

      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<int> { 3 }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var storedValue = await ActDbContext.Database.SqlQueryRaw<int>($"""SELECT "Column1" AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      storedValue.Should().Be(300);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([3]);
   }

   [Fact]
   public async Task Should_apply_value_changing_converter_of_DateTime()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<DateTime>(b => b.Property(t => t.Column1).HasConversion(v => v.Ticks, v => new DateTime(v, DateTimeKind.Utc)));

      var value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
      await using var tempTable = await SUT.BulkInsertValuesIntoTempTableAsync(new List<DateTime> { value }, new NpgsqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None }, CancellationToken.None);

      var storedValue = await ActDbContext.Database.SqlQueryRaw<long>($"""SELECT "Column1" AS "Value" FROM "{tempTable.Name}" """).SingleAsync();
      storedValue.Should().Be(value.Ticks);

      var values = await tempTable.Query.ToListAsync();
      values.Should().BeEquivalentTo([value]);
   }

   [Fact]
   public async Task Should_insert_null_for_struct_members_of_null_ComplexType_into_temp_table()
   {
      var withValues = TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0041"));
      var withNulls = TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0042"));

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([withValues, withNulls], new NpgsqlTempTableBulkInsertOptions());

      var loaded = await tempTable.Query.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls]);

      var structColumns = await TestEntityWithStructsInOptionalComplexType.QueryStructColumns(ActDbContext, $"\"{tempTable.Name}\"").ToListAsync();
      structColumns.Should().BeEquivalentTo(new[] { withValues, withNulls }.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
   }

   public readonly record struct Timestamp(long Value);

   public sealed class TimestampConverter() : ValueConverter<Timestamp, long>(v => v.Value, v => new Timestamp(v));

   public readonly record struct Quantity(int Value);

   public readonly record struct Moment(DateTime Value);

   public enum Color
   {
      None = 0,
      Blue = 1,
      Red = 2
   }
}
