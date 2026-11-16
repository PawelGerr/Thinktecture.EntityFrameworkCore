using System.Diagnostics;
using System.Globalization;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// The executor stages the rows in a temporary table ("CREATE TEMPORARY TABLE ... SELECT ... FROM target LIMIT 0"), updates the matching rows
// ("UPDATE target INNER JOIN staging"), deletes the matched rows from the staging table and inserts the remaining rows in input order
// ("INSERT INTO target ... SELECT ... FROM staging ORDER BY __ord"). EF Core executes these statements, so they are visible in ExecutedCommands.
// Only the INSERT statements that fill the staging table use raw ADO commands.
// The tests verify the resulting rows and the return values (the matched rows of the UPDATE plus the inserted rows).
// A new row that collides with another row on a unique index raises error 1062, like MERGE.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertOrUpdateAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("5B9587A3-2312-43DF-9681-38EC22AD8606");
   private static readonly Guid _MISSING_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkInsertOrUpdateAsync(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_entities_is_empty()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_not_throw_if_key_property_is_not_present_in_PropertiesToUpdate()
   {
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => new { entity.Name });

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new MySqlBulkInsertOrUpdateOptions { PropertiesToUpdate = propertiesProvider });

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_when_entity_has_no_key()
   {
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync(new List<KeylessTestEntity> { new() }, new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.KeylessTestEntity' has no primary key. Please provide key properties to perform JOIN/match on.");
   }

   [Fact]
   public async Task Should_insert_new_entities()
   {
      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1)
                    .And.Subject.First()
                    .Should().BeEquivalentTo(new TestEntity
                                             {
                                                Id = _TEST_ENTITY_ID,
                                                Name = "Name",
                                                RequiredName = "RequiredName",
                                                Count = 42
                                             });
   }

   [Fact]
   public async Task Should_update_existing_entities()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name";
      entity.Count = 42;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1)
                    .And.Subject.First()
                    .Should().BeEquivalentTo(entity);
   }

   [Fact]
   public async Task Should_insert_and_update_in_same_batch()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "UpdatedName";
      existingEntity.Count = 1;

      var newEntity = new TestEntity
                      {
                         Id = _OTHER_TEST_ENTITY_ID,
                         Name = "NewName",
                         RequiredName = "RequiredName",
                         Count = 2
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity]);
   }

   [Fact]
   public async Task Should_return_matched_and_inserted_rows()
   {
      var unchanged = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 };
      var changed = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(unchanged, changed);
      await ArrangeDbContext.SaveChangesAsync();

      changed.Count = 20;
      var newEntity = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 3 };

      // The UPDATE reports matched rows (the provider sets "UseAffectedRows=False"), so the unchanged row is counted as well.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([unchanged, changed, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([unchanged, changed, newEntity]);
   }

   [Fact]
   public async Task Should_insert_column_with_converter()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.ConvertibleClass = new ConvertibleClass(43);
      var newEntity = new TestEntity
                      {
                         Id = _OTHER_TEST_ENTITY_ID,
                         RequiredName = "RequiredName",
                         ConvertibleClass = new ConvertibleClass(42)
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var entities = await AssertDbContext.TestEntities.ToListAsync();
      entities.Should().BeEquivalentTo([
                                          new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(43) },
                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(42) }
                                       ]);
   }

   [Fact]
   public async Task Should_insert_private_property()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.SetPrivateField(1);

      var newEntity = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName" };
      newEntity.SetPrivateField(3);

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([newEntity, existingEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities.Single(e => e.Id == _TEST_ENTITY_ID).GetPrivateField().Should().Be(1);
      loadedEntities.Single(e => e.Id == _OTHER_TEST_ENTITY_ID).GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      var existingEntity = new TestEntityWithShadowProperties { Id = _TEST_ENTITY_ID };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      ActDbContext.Entry(existingEntity).Property("ShadowStringProperty").CurrentValue = "value1";
      ActDbContext.Entry(existingEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var newEntity = new TestEntityWithShadowProperties { Id = _OTHER_TEST_ENTITY_ID };
      ActDbContext.Entry(newEntity).Property("ShadowStringProperty").CurrentValue = "value2";
      ActDbContext.Entry(newEntity).Property("ShadowIntProperty").CurrentValue = 43;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([newEntity, existingEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithShadowProperties.ToListAsync();
      loadedEntities.Should().HaveCount(2);

      var existingEntry = AssertDbContext.Entry(loadedEntities.Single(e => e.Id == _TEST_ENTITY_ID));
      existingEntry.Property("ShadowStringProperty").CurrentValue.Should().Be("value1");
      existingEntry.Property("ShadowIntProperty").CurrentValue.Should().Be(42);

      var newEntry = AssertDbContext.Entry(loadedEntities.Single(e => e.Id == _OTHER_TEST_ENTITY_ID));
      newEntry.Property("ShadowStringProperty").CurrentValue.Should().Be("value2");
      newEntry.Property("ShadowIntProperty").CurrentValue.Should().Be(43);
   }

   [Fact]
   public async Task Should_insert_specified_properties_only()
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42,
                          PropertyWithBackingField = 7
                       };
      testEntity.SetPrivateField(3);

      // PropertiesToUpdate is not set. As with the other providers, the default must not require the properties that are not inserted.
      await SUT.BulkInsertOrUpdateAsync([testEntity],
                                        new MySqlBulkInsertOrUpdateOptions
                                        {
                                           PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                        });

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              RequiredName = "RequiredName",
                                              Count = 42,
                                              PropertyWithBackingField = 7
                                           });
      loadedEntity.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_and_update_specified_properties_only()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name"; // would neither be updated nor inserted
      existingEntity.Count = 42;
      var newEntity = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "New", RequiredName = "RequiredName", Count = 1 };

      var requiredProperties = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties());
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = requiredProperties,
                                                              PropertiesToUpdate = requiredProperties
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, (string?)null, 1),
                                                                         (_TEST_ENTITY_ID, "original value", 42));
   }

   [Fact]
   public async Task Should_update_entities()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = "Name";
      entity_1.Count = 1;
      entity_2.Name = "OtherName";
      entity_2.Count = 2;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity_1, entity_2], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_update_provided_entity_only()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = "Name";
      entity_1.Count = 1;
      entity_2.Name = "OtherName";
      entity_2.Count = 2;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity_1], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      entity_2.Name = default;
      entity_2.Count = default;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_update_specified_properties_only()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name"; // this would not be updated
      entity.Count = 42;
      entity.PropertyWithBackingField = 7;
      entity.SetPrivateField(3);

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                                           });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Count = 42,
                                              PropertyWithBackingField = 7,
                                              Name = "original value",
                                              RequiredName = "RequiredName"
                                           });
      loadedEntity.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_update_nullable_columns_to_null()
   {
      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      NullableCount = 42,
                      ConvertibleClass = new ConvertibleClass(43)
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = null;
      entity.NullableCount = null;
      entity.ConvertibleClass = null;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().BeNull();
      loadedEntity.NullableCount.Should().BeNull();
      loadedEntity.ConvertibleClass.Should().BeNull();
   }

   [Fact]
   public async Task Should_not_update_columns_of_the_primary_key_by_default()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName", Count = 1 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 42 }],
                                                           new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 42 });
   }

   [Fact]
   public async Task Should_only_insert_new_rows_if_no_property_is_left_to_update()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 42 };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "changed";
      existingEntity.Count = 43;
      var newEntity = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "new", RequiredName = "RequiredName", Count = 1 };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Empty
                                                           });

      // No column is left to update, so the UPDATE is skipped and only the inserted row is counted.
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new", 1),
                                                                         (_TEST_ENTITY_ID, "Name", 42));
   }

   [Fact]
   public async Task Should_only_insert_new_rows_if_the_key_is_the_only_property_to_update()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 42 };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "changed";
      var newEntity = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "new", RequiredName = "RequiredName", Count = 1 };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Id)
                                                           });

      // No column is left to update, so the UPDATE is skipped and only the inserted row is counted.
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new", 1),
                                                                         (_TEST_ENTITY_ID, "Name", 42));
   }

   [Fact]
   public async Task Should_throw_if_new_row_by_key_properties_collides_with_the_primary_key_of_another_row()
   {
      var matching = new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 1 };
      var differentKey = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "value", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(matching, differentKey);
      await ArrangeDbContext.SaveChangesAsync();

      // The rows are matched by the key property (Name) only; null equals null. The second row matches no row by its name,
      // so it is inserted and collides with the primary key of the existing row.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 10 },
                                                     new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "other value", RequiredName = "RequiredName", Count = 20 },
                                                     new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "new", RequiredName = "RequiredName", Count = 30 }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions
                                                  {
                                                     KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name)
                                                  });

      // 1062: duplicate entry for the primary key
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_TEST_ENTITY_ID, (string?)null, 1),
                                                                         (_OTHER_TEST_ENTITY_ID, "value", 2));
   }

   [Fact]
   public async Task Should_match_null_keys_null_safe()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 0 };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      // The first row matches the row with the NULL key, the second row matches no row and is inserted.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 10 },
                                                              new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 7, Count = 30 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.NullableCount),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.NullableCount, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, 5, 0),
                                                                                  (_TEST_ENTITY_ID, (int?)null, 10),
                                                                                  (_THIRD_TEST_ENTITY_ID, 7, 30));
   }

   [Fact]
   public async Task Should_upsert_entities_with_composite_primary_key()
   {
      ArrangeDbContext.CompositeEntities.AddRange(new MySqlCompositeEntity { RegionId = 1, ItemId = 1, Name = "1-1" },
                                                  new MySqlCompositeEntity { RegionId = 1, ItemId = 2, Name = "1-2" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new MySqlCompositeEntity { RegionId = 1, ItemId = 1, Name = "changed 1-1" },
                                                              new MySqlCompositeEntity { RegionId = 1, ItemId = 3, Name = "new 1-3" },
                                                              new MySqlCompositeEntity { RegionId = 2, ItemId = 1, Name = "new 2-1" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.CompositeEntities.OrderBy(e => e.RegionId).ThenBy(e => e.ItemId).ToListAsync();
      loadedEntities.Select(e => (e.RegionId, e.ItemId, e.Name)).Should().Equal((1, 1, "changed 1-1"),
                                                                                (1, 2, "1-2"),
                                                                                (1, 3, "new 1-3"),
                                                                                (2, 1, "new 2-1"));
   }

   [Fact]
   public async Task Should_throw_if_entities_contain_duplicate_keys()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // The staging table has a unique index on the key columns.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                                     new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 },
                                                     new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 3 }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions());

      // 1062: duplicate entry for the unique index of the staging table
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_and_update_TestEntity_with_ComplexType()
   {
      var testEntity_1 = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"), new BoundaryValueObject(2, 5));

      ArrangeDbContext.Add(testEntity_1);
      await ArrangeDbContext.SaveChangesAsync();

      testEntity_1.Boundary = new BoundaryValueObject(10, 20);
      var testEntity_2 = new TestEntityWithComplexType(new Guid("67A9500B-CF51-4A39-8C89-F2EBF7EDE84D"), new BoundaryValueObject(3, 4));

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([testEntity_1, testEntity_2], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity_1, testEntity_2]);
   }

   [Fact]
   public async Task Should_insert_and_update_nullable_ComplexTypes_with_null_and_non_null_values()
   {
      var toNulls = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0001"));
      var toValues = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0002"));
      ArrangeDbContext.AddRange(toNulls, toValues);
      await ArrangeDbContext.SaveChangesAsync();

      var entities = new[]
                     {
                        TestEntityWithNullableComplexTypes.CreateWithNulls(toNulls.Id),
                        TestEntityWithNullableComplexTypes.CreateWithValues(toValues.Id),
                        TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0003")),
                        TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0004"))
                     };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(4);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync();
      loaded.Should().BeEquivalentTo(entities);
   }

   [Fact]
   public async Task Should_insert_and_update_struct_members_of_null_ComplexType_as_null()
   {
      var toNulls = TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0031"));
      var toValues = TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0032"));
      ArrangeDbContext.AddRange(toNulls, toValues);
      await ArrangeDbContext.SaveChangesAsync();

      var entities = new[]
                     {
                        TestEntityWithStructsInOptionalComplexType.CreateWithNulls(toNulls.Id),
                        TestEntityWithStructsInOptionalComplexType.CreateWithValues(toValues.Id),
                        TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0033")),
                        TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0034"))
                     };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(4);
      var loaded = await AssertDbContext.TestEntitiesWithStructsInOptionalComplexType.ToListAsync();
      loaded.Should().BeEquivalentTo(entities);

      var structColumns = await MySqlStructColumns.Query(AssertDbContext, "TestEntitiesWithStructsInOptionalComplexType").ToListAsync();
      structColumns.Should().BeEquivalentTo(entities.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
   }

   [Fact]
   public async Task Should_insert_and_update_flags_enum()
   {
      var existingEntity = new TestEntityWithFlags { Id = _TEST_ENTITY_ID, GroupId = 1, Phase = PhaseMembership.A };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.GroupId = 2;
      existingEntity.Phase = PhaseMembership.A | PhaseMembership.C;
      var newEntity = new TestEntityWithFlags { Id = _OTHER_TEST_ENTITY_ID, GroupId = 3, Phase = PhaseMembership.B | PhaseMembership.C };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithFlags.OrderBy(e => e.GroupId).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.GroupId, e.Phase)).Should().Equal((_TEST_ENTITY_ID, 2, PhaseMembership.A | PhaseMembership.C),
                                                                            (_OTHER_TEST_ENTITY_ID, 3, PhaseMembership.B | PhaseMembership.C));
   }

   [Fact]
   public async Task Should_insert_and_update_entities_with_different_column_names()
   {
      var existingEntity = new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Name 1", Count = 1 };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = null;
      existingEntity.Count = 10;
      var newEntity = new TestEntityWithDifferentColumnNames { Id = _OTHER_TEST_ENTITY_ID, Name = "Name 2", Count = 2 };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([newEntity, existingEntity], config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_update_entities_with_different_column_names_by_non_pk_key()
   {
      ArrangeDbContext.TestEntitiesWithDifferentColumnNames.Add(new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Name 1", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();

      // The first row matches by the key column (item_count) and is updated; the second row has a different key and is inserted.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithDifferentColumnNames { Id = _MISSING_TEST_ENTITY_ID, Name = "Changed", Count = 1 },
                                                              new TestEntityWithDifferentColumnNames { Id = _OTHER_TEST_ENTITY_ID, Name = "Inserted", Count = 2 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithDifferentColumnNames>(e => e.Count)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_TEST_ENTITY_ID, "Changed", 1),
                                                                         (_OTHER_TEST_ENTITY_ID, "Inserted", 2));
   }

   [Fact]
   public async Task Should_insert_and_update_entities_with_auto_increment_column()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name";
      var newEntity = new TestEntityWithAutoIncrement { Id = existingEntity.Id + 10, Name = "New Entity" };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity], config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_new_rows_with_generated_keys_if_the_auto_increment_key_is_not_selected_for_insert()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name";

      // The key is staged for the match, but it is not inserted, so the new rows get generated ids.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntityWithAutoIncrement { Name = "new 1" }, new TestEntityWithAutoIncrement { Name = "new 2" }],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name)
                                                           });

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("Name", "new 1", "new 2");
      loadedEntities[0].Id.Should().Be(existingEntity.Id);
   }

   [Fact]
   public async Task Should_update_property_that_is_not_selected_for_insert()
   {
      await ArrangeDbContext.GetService<MySqlBulkOperationExecutor>()
                            .BulkInsertAsync([new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 5, String = "value", NullableInt = 6, NullableString = "value" }],
                                             new MySqlBulkInsertOptions());

      // The insert and update properties are independent: the existing row gets the new NullableInt, the new row gets its SQL default value.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 50, String = "value", NullableInt = 60 },
                                                              new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 7, String = "value", NullableInt = 70 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new { e.Id, e.Int }),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.NullableInt)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.Int).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Int, e.NullableInt, e.String)).Should().Equal((_TEST_ENTITY_ID, 5, (int?)60, "value"),
                                                                                       (_OTHER_TEST_ENTITY_ID, 7, (int?)2, "3"));
   }

   [Fact]
   public async Task Should_upsert_if_a_key_property_is_not_selected_for_insert()
   {
      await ArrangeDbContext.GetService<MySqlBulkOperationExecutor>()
                            .BulkInsertAsync([new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 5, String = "key", NullableInt = 6, NullableString = "value" }],
                                             new MySqlBulkInsertOptions());

      // The rows are matched by "String", which is not inserted, so the new row gets the SQL default value of the column.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 50, String = "key" },
                                                              new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 7, String = "other key" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new { e.Id, e.Int }),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Int),
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.String)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.Int).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Int, e.String)).Should().Equal((_OTHER_TEST_ENTITY_ID, 7, "3"),
                                                                         (_TEST_ENTITY_ID, 50, "key"));
   }

   [Fact]
   public async Task Should_write_provided_column_values_as_is_despite_sql_default_value()
   {
      var entity = new TestEntityWithSqlDefaultValues
                   {
                      Id = _TEST_ENTITY_ID,
                      Int = 5,
                      String = "value",
                      NullableInt = 6,
                      NullableString = "otherValue"
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Int = 0;
      entity.String = "other value";
      entity.NullableInt = null;
      entity.NullableString = null;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Int = 0,               // persisted as-is
                                              NullableInt = null,    // persisted as-is
                                              String = "other value",
                                              NullableString = null  // persisted as-is
                                           });
   }

   [Fact]
   public async Task Should_use_sql_default_values_of_columns_that_are_not_inserted()
   {
      var existingEntity = new TestEntityWithSqlDefaultValues
                           {
                              Id = _TEST_ENTITY_ID,
                              Int = 5,
                              String = "value",
                              NullableInt = 6,
                              NullableString = "otherValue"
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID }, new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID }],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Id),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Empty
                                                           });

      // No column is left to update, so the UPDATE is skipped and only the inserted row is counted.
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.Int).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([
                                                new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 1, NullableInt = 2, String = "3", NullableString = "4" },
                                                existingEntity
                                             ],
                                             config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_and_update_entity_with_json_column()
   {
      var existingEntity = new TestEntityWithJsonColumns
                           {
                              Id = _TEST_ENTITY_ID,
                              JsonColumn = """{"key": "original"}"""
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.JsonColumn = """{"key": "updated"}""";

      var newEntity = new TestEntityWithJsonColumns
                      {
                         Id = _OTHER_TEST_ENTITY_ID,
                         JsonColumn = """{"key": "new"}"""
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      // MySQL normalizes the JSON text; the values are already in the normalized form.
      var loadedEntities = await AssertDbContext.TestEntitiesWithJsonColumns.OrderBy(e => e.JsonColumn).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.JsonColumn)).Should().Equal((_OTHER_TEST_ENTITY_ID, """{"key": "new"}"""),
                                                                      (_TEST_ENTITY_ID, """{"key": "updated"}"""));
   }

   [Fact]
   public async Task Should_accept_options_of_other_providers()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name"; // this would not be updated
      existingEntity.Count = 42;
      var newEntity = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "New", RequiredName = "RequiredName", Count = 1 };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                           new GenericBulkOperationOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties()),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, (string?)null, 1),
                                                                         (_TEST_ENTITY_ID, "original value", 42));
   }

   [Fact]
   public async Task Should_throw_if_a_new_row_collides_with_another_row_on_a_unique_index()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "a" }, new MySqlTestEntity { Id = 2, Name = "b" });
      await ArrangeDbContext.SaveChangesAsync();

      // The name of MySqlTestEntity has a unique index. The first row collides with row 1 on that index, but its primary key is new.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new MySqlTestEntity { Id = 9, Name = "a" },
                                                     new MySqlTestEntity { Id = 2, Name = "b changed" },
                                                     new MySqlTestEntity { Id = 10, Name = "new" }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = 1 });

      // 1062: duplicate entry for the unique index; the update of row 2 is rolled back as well.
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      var loadedEntities = await AssertDbContext.Entities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name)).Should().Equal((1, "a"), (2, "b"));
   }

   [Fact]
   public async Task Should_insert_or_update_entities_in_table_name_override()
   {
      await CreateRedirectTableAsync("TestEntities_BulkUpsertRedirect");

      try
      {
         var existingEntity = new TestEntity
                              {
                                 Id = _TEST_ENTITY_ID,
                                 RequiredName = "RequiredName",
                                 Name = "UpdatedName",
                                 Count = 99
                              };
         var newEntity = new TestEntity
                         {
                            Id = _OTHER_TEST_ENTITY_ID,
                            RequiredName = "RequiredName",
                            Name = "NewName",
                            Count = 1
                         };

         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                              new MySqlBulkInsertOrUpdateOptions { TableName = "TestEntities_BulkUpsertRedirect" });

         affectedRows.Should().Be(2);

         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         (await LoadColumnAsync<string>("TestEntities_BulkUpsertRedirect", "Name")).Should().BeEquivalentTo(["NewName", "UpdatedName"]);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpsertRedirect");
      }
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_or_update_entities_in_table_from_table_name_provider(bool useGenericOptions)
   {
      await CreateRedirectTableAsync("TestEntities_BulkUpsertRedirect");

      try
      {
         var existingEntity = new TestEntity
                              {
                                 Id = _TEST_ENTITY_ID,
                                 RequiredName = "RequiredName",
                                 Name = "UpdatedName",
                                 Count = 99
                              };
         var newEntity = new TestEntity
                         {
                            Id = _OTHER_TEST_ENTITY_ID,
                            RequiredName = "RequiredName",
                            Name = "NewName",
                            Count = 1
                         };
         var provider = new SuffixTableNameProvider("_BulkUpsertRedirect");
         IBulkInsertOrUpdateOptions options = useGenericOptions
                                                 ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                                 : new MySqlBulkInsertOrUpdateOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], options);

         affectedRows.Should().Be(2);

         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         (await LoadColumnAsync<string>("TestEntities_BulkUpsertRedirect", "Name")).Should().BeEquivalentTo(["NewName", "UpdatedName"]);
         provider.RequestedEntityTypes.Should().ContainSingle();
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpsertRedirect");
      }
   }

   [Fact]
   public async Task Should_insert_or_update_entities_in_table_of_the_database_from_ForRootEntity_provider_if_SchemaBehavior_is_Ignore()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database (here: the current one).
      // SchemaBehavior Ignore drops the schema; the default (Throw) rejects it.
      ConfigureMySql = builder => builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
      var database = await ActDbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() AS `Value`").SingleAsync();
      await CreateRedirectTableAsync("TestEntities_BulkUpsertRedirect");

      try
      {
         var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                                 new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" },
                                                                 new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "NewName" }
                                                              ],
                                                              new MySqlBulkInsertOrUpdateOptions
                                                              {
                                                                 TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkUpsertRedirect", database)
                                                              });

         affectedRows.Should().Be(2);

         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         (await LoadColumnAsync<string>("TestEntities_BulkUpsertRedirect", "Name")).Should().BeEquivalentTo(["NewName", "UpdatedName"]);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpsertRedirect");
      }
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_or_update_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      // The entity is keyless, but the missing table name is the problem to report (as the other providers do).
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync(new List<TempTable<int>> { new(0) }, new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_or_update_keyed_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync(new[] { new TestEntityTempTable { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" } }, new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_insert_or_update_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var existingEntity = new TestEntity
                           {
                              Id = _TEST_ENTITY_ID,
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntity = new TestEntityTempTable
                          {
                             Id = _TEST_ENTITY_ID,
                             Name = "UpdatedName",
                             RequiredName = "RequiredName",
                             Count = 99
                          };
      var newEntity = new TestEntityTempTable
                      {
                         Id = _OTHER_TEST_ENTITY_ID,
                         Name = "NewName",
                         RequiredName = "RequiredName",
                         Count = 1
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { updatedEntity, newEntity }, new MySqlBulkInsertOrUpdateOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "NewName", 1),
                                                                         (_TEST_ENTITY_ID, "UpdatedName", 99));
   }

   [Fact]
   public async Task Should_insert_or_update_temp_table_entity_with_ForRootEntity_provider()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var existingEntity = new TestEntity
                           {
                              Id = _TEST_ENTITY_ID,
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntity = new TestEntityTempTable
                          {
                             Id = _TEST_ENTITY_ID,
                             Name = "UpdatedName",
                             RequiredName = "RequiredName",
                             Count = 99
                          };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { updatedEntity },
                                                           new MySqlBulkInsertOrUpdateOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities") });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("UpdatedName");
      loadedEntity.Count.Should().Be(99);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_inlined_owned_type()
   {
      ArrangeDbContext.TestEntities_Own_Inline.Add(new TestEntity_Owns_Inline { Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), InlineEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_Inline.SingleAsync();
      entity.InlineEntity.IntColumn = 2;

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("Temp tables don't support owned entities.");

      (await AssertDbContext.TestEntities_Own_Inline.SingleAsync()).InlineEntity.IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_owned_type_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateOne.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"), SeparateEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateOne.SingleAsync();
      entity.SeparateEntity!.IntColumn = 2;

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("*owned entities mapped to separate tables*");

      (await AssertDbContext.TestEntities_Own_SeparateOne.SingleAsync()).SeparateEntity!.IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_owned_collection_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateMany.Add(new TestEntity_Owns_SeparateMany { Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"), SeparateEntities = [new() { IntColumn = 1, StringColumn = "old" }] });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateMany.SingleAsync();
      entity.SeparateEntities[0].IntColumn = 2;

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("*owned entities mapped to separate tables*");

      (await AssertDbContext.TestEntities_Own_SeparateMany.SingleAsync()).SeparateEntities.Single().IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_separate_owned_type_inside_inlined_owned_type()
   {
      var entity = new TestEntity_Owns_Inline_SeparateOne
                   {
                      Id = _TEST_ENTITY_ID,
                      InlineEntity = new OwnedEntity_Owns_SeparateOne { IntColumn = 1, StringColumn = "old", SeparateEntity = new OwnedEntity { IntColumn = 10, StringColumn = "old inner" } }
                   };
      ArrangeDbContext.TestEntities_Own_Inline_SeparateOne.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.InlineEntity.IntColumn = 2;
      entity.InlineEntity.SeparateEntity!.IntColumn = 20;

      // The separately mapped owned type is not nested in the root entity directly, but it must not end up in the INSERT statement of the root table either.
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>();

      var loadedEntity = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.SingleAsync();
      loadedEntity.InlineEntity.IntColumn.Should().Be(1);
      loadedEntity.InlineEntity.SeparateEntity!.IntColumn.Should().Be(10);
   }

   [Fact]
   public async Task Should_throw_on_update_of_separate_owned_type_inside_inlined_owned_type()
   {
      var entity = new TestEntity_Owns_Inline_SeparateOne
                   {
                      Id = _TEST_ENTITY_ID,
                      InlineEntity = new OwnedEntity_Owns_SeparateOne { IntColumn = 1, StringColumn = "old", SeparateEntity = new OwnedEntity { IntColumn = 10, StringColumn = "old inner" } }
                   };
      ArrangeDbContext.TestEntities_Own_Inline_SeparateOne.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.InlineEntity.IntColumn = 2;
      entity.InlineEntity.SeparateEntity!.IntColumn = 20;

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>();

      var loadedEntity = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.SingleAsync();
      loadedEntity.InlineEntity.IntColumn.Should().Be(1);
      loadedEntity.InlineEntity.SeparateEntity!.IntColumn.Should().Be(10);
   }

   // The tests below cover behavior specific to the MySQL executor: staging table, batching, transactions, command timeout.

   [Fact]
   public async Task Should_stage_rows_with_the_column_definitions_of_the_target_table()
   {
      ArrangeDbContext.TestEntitiesWithCollation.AddRange(new TestEntityWithCollation(_TEST_ENTITY_ID, "old 1", "abc"),
                                                          new TestEntityWithCollation(_OTHER_TEST_ENTITY_ID, "old 2", "ABC"));
      await ArrangeDbContext.SaveChangesAsync();

      // The key column is case-sensitive (utf8mb4_bin) and the database default is not. The staging table copies the collation of the column,
      // so its unique key index accepts all keys, each input row matches its own row only, and "aBc" is a new row.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithCollation(_MISSING_TEST_ENTITY_ID, "new 2", "ABC"),
                                                              new TestEntityWithCollation(_MISSING_TEST_ENTITY_ID, "new 1", "abc"),
                                                              new TestEntityWithCollation(_THIRD_TEST_ENTITY_ID, "new 3", "aBc")
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithCollation),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithoutCollation)
                                                           });

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntitiesWithCollation.OrderBy(e => e.ColumnWithCollation).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.ColumnWithoutCollation, e.ColumnWithCollation)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new 2", "ABC"),
                                                                                                          (_THIRD_TEST_ENTITY_ID, "new 3", "aBc"),
                                                                                                          (_TEST_ENTITY_ID, "new 1", "abc"));
   }

   [Fact]
   public async Task Should_throw_if_entities_contain_duplicate_non_null_values_of_nullable_key()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // The staging table has a unique index on the nullable key column.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 1 },
                                                     new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 2 }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions
                                                  {
                                                     KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.NullableCount),
                                                     PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                                  });

      // 1062: duplicate entry for the unique index of the staging table
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_match_null_keys_null_safe_through_unique_staging_index()
   {
      // SaveChanges would replace NULL with the SQL default values of the columns; the bulk insert writes the values as they are.
      await ArrangeDbContext.GetService<MySqlBulkOperationExecutor>()
                            .BulkInsertAsync([
                                                new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 1, String = "a", NullableInt = null, NullableString = "x" },
                                                new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 2, String = "b", NullableInt = 1, NullableString = null },
                                                new TestEntityWithSqlDefaultValues { Id = _THIRD_TEST_ENTITY_ID, Int = 3, String = "c", NullableInt = 1, NullableString = "x" }
                                             ],
                                             new MySqlBulkInsertOptions());

      // The keys differ only in the part that is NULL. The unique index of the staging table accepts all of them, and "<=>" matches NULL with NULL.
      // The last row has a NULL key part and no matching row, so it is inserted.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 10, String = "a", NullableInt = null, NullableString = "x" },
                                                              new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 20, String = "b", NullableInt = 1, NullableString = null },
                                                              new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 30, String = "c", NullableInt = 1, NullableString = "x" },
                                                              new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 40, String = "d", NullableInt = null, NullableString = "y" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new { e.NullableInt, e.NullableString }),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Int)
                                                           });

      affectedRows.Should().Be(4);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.String).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Int, e.NullableInt, e.NullableString)).Should().Equal((_TEST_ENTITY_ID, 10, (int?)null, "x"),
                                                                                               (_OTHER_TEST_ENTITY_ID, 20, (int?)1, (string?)null),
                                                                                               (_THIRD_TEST_ENTITY_ID, 30, (int?)1, "x"),
                                                                                               (_MISSING_TEST_ENTITY_ID, 40, (int?)null, "y"));
   }

   [Fact]
   public async Task Should_insert_rows_in_input_order_if_child_key_sorts_before_parent_key()
   {
      var parentId = new Guid("FFFFFFFF-0000-0000-0000-000000000001");
      var childId = new Guid("00000000-0000-0000-0000-000000000001");

      // InnoDB checks foreign keys row by row. The staging table has a unique index on the key, and without "ORDER BY __ord"
      // the child row (its key sorts first) would be inserted before its parent (error 1452).
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = parentId, RequiredName = "Parent" },
                                                              new TestEntity { Id = childId, RequiredName = "Child", ParentId = parentId }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      // The INSERT lists the columns of the target; the order column "__ord" is used for the ORDER BY only.
      var insertStatement = ExecutedCommands.Should().ContainSingle(command => command.StartsWith("INSERT INTO `TestEntities`", StringComparison.Ordinal)).Subject;
      insertStatement.Should().EndWith("ORDER BY s.`__ord`;");
      insertStatement[..insertStatement.IndexOf(" ORDER BY", StringComparison.Ordinal)].Should().NotContain("__ord");

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.ParentId)).Should().Equal((childId, (Guid?)parentId), (parentId, (Guid?)null));
   }

   [Fact]
   public async Task Should_throw_if_an_existing_row_is_updated_to_reference_a_new_row_of_the_same_batch()
   {
      var existingId = new Guid("00000000-0000-0000-0000-000000000001");
      var newId = new Guid("FFFFFFFF-0000-0000-0000-000000000001");
      ArrangeDbContext.Add(new TestEntity { Id = existingId, RequiredName = "Existing" });
      await ArrangeDbContext.SaveChangesAsync();

      // Known limitation: the UPDATE runs before the INSERT, and InnoDB checks foreign keys row by row, so the new parent does not exist yet.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = existingId, RequiredName = "Existing", ParentId = newId },
                                                     new TestEntity { Id = newId, RequiredName = "New" }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions());

      // 1452: cannot add or update a child row, a foreign key constraint fails
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1452);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Id.Should().Be(existingId);
      loadedEntity.ParentId.Should().BeNull();
   }

   [Fact]
   public async Task Should_drop_the_staging_table_if_the_upsert_fails()
   {
      // A temp table belongs to the session. The connection stays open, so both upserts share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                                     new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions());
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      // The second upsert leases the same name again. If the staging table of the failed upsert still existed, its CREATE would fail with error 1050.
      (await SUT.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 3 }], new MySqlBulkInsertOrUpdateOptions())).Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(3);
   }

   [Fact]
   public async Task Should_update_and_upsert_if_session_uses_safe_updates()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      // The staging table of an AUTO_INCREMENT key has no key index in an upsert. With "sql_safe_updates" MySQL rejects an UPDATE or DELETE
      // that reads a table without a condition on a key (error 1175).
      await ActDbContext.Database.OpenConnectionAsync();

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("SET SESSION sql_safe_updates = 1");

         existingEntity.Name = "updated";
         (await SUT.BulkUpdateAsync([existingEntity], new MySqlBulkUpdateOptions())).Should().Be(1);

         existingEntity.Name = "upserted";
         (await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntityWithAutoIncrement { Name = "new" }], new MySqlBulkInsertOrUpdateOptions())).Should().Be(2);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("SET SESSION sql_safe_updates = 0");
      }

      ExecutedCommands.Should().Contain(command => command.StartsWith("UPDATE /*+ SET_VAR(sql_safe_updates = 0) */ `TestEntitiesWithAutoIncrement` AS d", StringComparison.Ordinal));
      ExecutedCommands.Should().Contain(command => command.StartsWith("DELETE /*+ SET_VAR(sql_safe_updates = 0) */ s FROM", StringComparison.Ordinal));

      var loadedNames = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync();
      loadedNames.Should().Equal("upserted", "new");
   }

   [Fact]
   public async Task Should_generate_auto_increment_keys_in_input_order()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithAutoIncrement { Name = "first" },
                                                              new TestEntityWithAutoIncrement { Name = "second" },
                                                              new TestEntityWithAutoIncrement { Name = "third" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(3);

      var loadedNames = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync();
      loadedNames.Should().Equal("first", "second", "third");
   }

   [Fact]
   public async Task Should_insert_several_new_rows_with_generated_keys()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "changed";

      // The staging table has no key index for a store-generated key, so several new rows with the id 0 can be staged.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithAutoIncrement { Name = "new 1" },
                                                              existingEntity,
                                                              new TestEntityWithAutoIncrement { Name = "new 2" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("changed", "new 1", "new 2");
      loadedEntities[0].Id.Should().Be(existingEntity.Id);
      loadedEntities.Select(e => e.Id).Should().OnlyHaveUniqueItems().And.NotContain(0);
   }

   [Fact]
   public async Task Should_throw_if_new_rows_share_the_default_value_of_a_non_generated_key()
   {
      ArrangeDbContext.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      // The key is not inserted, but it is staged and the staging table has a unique index on it.
      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntity { Id = Guid.Empty, RequiredName = "new 1" },
                                                     new TestEntity { Id = Guid.Empty, RequiredName = "new 2" }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions
                                                  {
                                                     PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => e.RequiredName),
                                                     PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.RequiredName)
                                                  });

      // 1062: duplicate entry for the unique index of the staging table
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.SingleAsync()).Id.Should().Be(_TEST_ENTITY_ID);
   }

   [Fact]
   public async Task Should_update_all_updatable_properties_if_PropertiesToUpdate_is_null_and_PropertiesToInsert_is_restricted()
   {
      await ArrangeDbContext.GetService<MySqlBulkOperationExecutor>()
                            .BulkInsertAsync([new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 5, String = "value", NullableInt = 6, NullableString = "value" }],
                                             new MySqlBulkInsertOptions());

      // The existing row gets all updatable properties, also the ones outside the insert set. The new row gets the SQL default values of the other columns.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 50, String = "changed", NullableInt = 60, NullableString = "changed" },
                                                              new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 7, String = "new", NullableInt = 70, NullableString = "new" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new { e.Id, e.Int })
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.Int).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Int, e.NullableInt, e.String, e.NullableString)).Should().Equal((_OTHER_TEST_ENTITY_ID, 7, (int?)2, "3", (string?)"4"),
                                                                                                         (_TEST_ENTITY_ID, 50, (int?)60, "changed", (string?)"changed"));
   }

   // The staging table has the columns Id and Name, so the default budget of 5,000 parameters gives 2,500 rows per statement.
   // The final INSERT ... SELECT from the staging table is counted in Com_insert_select, not in Com_insert.
   [Theory]
   [InlineData(1, null, 1)]
   [InlineData(2500, null, 1)]
   [InlineData(2501, null, 2)]
   [InlineData(5001, null, 3)]
   [InlineData(7, 2, 7)]
   [InlineData(7, 4, 4)]
   [InlineData(7, 1, 7)] // a budget below the column count gives 1 row per statement
   [InlineData(7, 200, 1)]
   public async Task Should_split_rows_into_statements_according_to_the_parameter_budget(int rowCount, int? maxParametersPerStatement, int expectedStatements)
   {
      // every second row exists already, so each batch inserts and updates rows
      ArrangeDbContext.TestEntitiesWithAutoIncrement.AddRange(Enumerable.Range(1, rowCount).Where(id => id % 2 == 0).Select(id => new TestEntityWithAutoIncrement { Id = id, Name = $"old {id}" }));
      await ArrangeDbContext.SaveChangesAsync();

      var entities = Enumerable.Range(1, rowCount).Select(id => new TestEntityWithAutoIncrement { Id = id, Name = $"new {id}" }).ToList();

      // The status counters are per session. The connection stays open, so the executor and the status queries share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = maxParametersPerStatement });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(rowCount);
      statements.Should().Be(expectedStatements);

      var loadedNames = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync();
      loadedNames.Should().Equal(Enumerable.Range(1, rowCount).Select(id => $"new {id}"));
   }

   [Fact]
   public async Task Should_limit_rows_per_statement_to_the_parameter_limit_of_mysql()
   {
      // MySQL accepts 65,535 parameters per statement, so the number of rows per statement is limited by the number of columns.
      var columnCount = ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetProperties().Count();
      var maximumRows = 65_535 / columnCount;
      var entities = Enumerable.Range(0, maximumRows + 1).Select(count => new TestEntity { Id = Guid.NewGuid(), RequiredName = "RequiredName", Count = count });
      await ActDbContext.Database.OpenConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = 65_535 });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(maximumRows + 1);
      statements.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(maximumRows + 1);
   }

   [Fact]
   public async Task Should_upsert_row_that_references_another_row_of_the_same_batch()
   {
      var parent = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Parent" };
      var child = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Child", ParentId = _TEST_ENTITY_ID };

      // MySQL validates foreign keys row by row, so the parent has to come first.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([parent, child], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.ParentId)).Should().Equal((_OTHER_TEST_ENTITY_ID, (Guid?)_TEST_ENTITY_ID),
                                                                    (_TEST_ENTITY_ID, (Guid?)null));
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(65_536)]
   public void Should_throw_if_the_parameter_budget_is_out_of_range(int maxParametersPerStatement)
   {
      var act = () => new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = maxParametersPerStatement };

      act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
   }

   [Fact]
   public async Task Should_upsert_entities_with_command_timeout()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }],
                                                           new MySqlBulkInsertOrUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(30) });

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_apply_command_timeout_to_upsert_statements()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      // The uncommitted update of another transaction locks the row until the lock wait timeout of the server (50 seconds by default) is reached.
      await using var blockingTransaction = await ArrangeDbContext.Database.BeginTransactionAsync();
      await ArrangeDbContext.Database.ExecuteSqlRawAsync("UPDATE `TestEntities` SET `Name` = 'Blocking' WHERE `Id` = {0}", _TEST_ENTITY_ID);

      var stopwatch = Stopwatch.StartNew();

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new MySqlBulkInsertOrUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(1) }))
               .Should().ThrowAsync<MySqlException>();

      stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
   }

   [Fact]
   public async Task Should_throw_when_cancellation_is_requested()
   {
      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }], new MySqlBulkInsertOrUpdateOptions(), cancellationTokenSource.Token))
               .Should().ThrowAsync<OperationCanceledException>();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_apply_updates_and_inserts_of_earlier_batches_if_a_later_batch_fails()
   {
      var existingEntity = new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 5, String = "old", NullableInt = 6, NullableString = "old" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var act = () => SUT.BulkInsertOrUpdateAsync([
                                                     new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, Int = 7, String = "new", NullableInt = 8, NullableString = "new" },
                                                     new TestEntityWithSqlDefaultValues { Id = _OTHER_TEST_ENTITY_ID, Int = 9, String = "inserted", NullableInt = 10, NullableString = "inserted" },
                                                     new TestEntityWithSqlDefaultValues { Id = _THIRD_TEST_ENTITY_ID, Int = 11, String = new string('x', 101), NullableInt = 12, NullableString = "too long" }
                                                  ],
                                                  new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = 1 });

      // 1406: data too long for the column (strict mode)
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1406);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(existingEntity);
   }

   [Fact]
   public async Task Should_upsert_rows_and_commit_with_the_transaction_of_the_caller()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Count = 42;
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 }], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities.AsNoTracking().ToListAsync()).Should().ContainSingle().Which.Count.Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.CommitAsync();

      var loadedEntities = await AssertDbContext.TestEntities.AsNoTracking().OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, 1), (_TEST_ENTITY_ID, 42));
   }

   [Fact]
   public async Task Should_not_upsert_rows_if_the_transaction_of_the_caller_is_rolled_back()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Count = 42;
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 }], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      await transaction.RollbackAsync();

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_upsert_rows_and_commit_with_the_transaction_scope()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Count = 42;
      UseConnectionWithoutXaTransactions();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 }], new MySqlBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);
         scope.Complete();
      }

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, 1), (_TEST_ENTITY_ID, 42));
   }

   [Fact]
   public async Task Should_roll_back_the_upsert_if_the_transaction_scope_is_not_completed()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Count = 42;
      UseConnectionWithoutXaTransactions();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 }], new MySqlBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);
      }

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.Count.Should().Be(0);
   }

   // MySQL rejects temporary tables inside XA transactions. Without XA, MySqlConnector enlists the connection with a plain "START TRANSACTION".
   private void UseConnectionWithoutXaTransactions()
   {
      var builder = new MySqlConnectionStringBuilder(ConnectionString) { UseXaTransactions = false };
      ActDbContext.Database.SetConnectionString(builder.ConnectionString);
   }

   // The status counters are per session; see Should_split_rows_into_statements_according_to_batch_size.
   private async Task<long> GetSessionStatusAsync(string name)
   {
      await using var command = ActDbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = $"SHOW SESSION STATUS LIKE '{name}'";
      await using var reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();

      return Int64.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
   }

   // Creates a copy of the table "TestEntities" that contains one row, and empties the original table, so that the upsert is visible in the copy only.
   private async Task CreateRedirectTableAsync(string tableName)
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "OldName" });
      await ArrangeDbContext.SaveChangesAsync();

      // A run that was killed before its cleanup leaves the table behind; CREATE TABLE would then fail with error 1050.
      await DropTablesAsync(tableName);
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(tableName)} LIKE `TestEntities`");
      await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(tableName)} SELECT * FROM `TestEntities`");
      await ActDbContext.Database.ExecuteSqlRawAsync("DELETE FROM `TestEntities`");
   }

   private async Task<List<T>> LoadColumnAsync<T>(string tableName, string columnName)
   {
      return await AssertDbContext.Database.SqlQueryRaw<T>($"SELECT {Quote(columnName)} AS `Value` FROM {Quote(tableName)}").ToListAsync();
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
}
