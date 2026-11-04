using System.Diagnostics;
using System.Globalization;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// The executor writes with batched "INSERT ... ON DUPLICATE KEY UPDATE" statements using raw ADO commands, so the statements are not visible in ExecutedCommands.
// The tests verify the resulting rows and the return values (the number of processed input rows, not the 1/2/0 affected-row codes of MySQL).
// MySQL fires ON DUPLICATE KEY for any primary key or unique index conflict, so each assignment is guarded by the key properties:
// col = IF(<key equals incoming key, null-safe>, <incoming value>, col). Key columns are never updated and a row that collides with
// a different row on another unique index is skipped silently.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertOrUpdateAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("5B9587A3-2312-43DF-9681-38EC22AD8606");
   private static readonly Guid _MISSING_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");

   // Before MySQL 8.0.19 (and always on MariaDB) there is no row alias, so the executor falls back to the deprecated VALUES(col).
   private static readonly MySqlServerVersion _SERVER_VERSION_WITHOUT_ROW_ALIAS = new(new Version(8, 0, 18));

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkInsertOrUpdateAsync(MySqlContainerFixture fixture)
      : base(fixture)
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
   public async Task Should_return_the_number_of_processed_rows_including_unchanged_rows()
   {
      var unchanged = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 };
      var changed = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(unchanged, changed);
      await ArrangeDbContext.SaveChangesAsync();

      changed.Count = 20;
      var newEntity = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 3 };

      // MySQL reports different affected-row codes for inserted, updated and unchanged rows; the executor reports the number of input rows instead.
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

      affectedRows.Should().Be(2);

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

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new", 1),
                                                                         (_TEST_ENTITY_ID, "Name", 42));
   }

   [Fact]
   public async Task Should_update_only_rows_whose_primary_key_and_key_properties_match()
   {
      var matching = new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 1 };
      var differentKey = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "value", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(matching, differentKey);
      await ArrangeDbContext.SaveChangesAsync();

      // The primary key is the only unique index of the table, so ON DUPLICATE KEY fires for the rows with an existing Id.
      // They are updated only if the key property (Name) is equal as well; null equals null.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 10 },
                                                              new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "other value", RequiredName = "RequiredName", Count = 20 },
                                                              new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "new", RequiredName = "RequiredName", Count = 30 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name)
                                                           });

      affectedRows.Should().Be(3, "skipped rows are counted as processed");

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "value", 2),
                                                                         (_TEST_ENTITY_ID, (string?)null, 10),
                                                                         (_THIRD_TEST_ENTITY_ID, "new", 30));
   }

   [Fact]
   public async Task Should_match_null_keys_null_safe()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 0 };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 10 },
                                                              new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 20 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.NullableCount),
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.NullableCount).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.NullableCount, e.Count)).Should().Equal((_TEST_ENTITY_ID, (int?)null, 10),
                                                                                  (_OTHER_TEST_ENTITY_ID, 5, 0));
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
   public async Task Should_apply_duplicates_of_a_key_in_order()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 },
                                                              new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 3 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions());

      // The first row is inserted, the second row finds it and updates it (last one wins).
      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Count)).Should().Equal((_TEST_ENTITY_ID, 2), (_OTHER_TEST_ENTITY_ID, 3));
   }

   [Fact]
   public async Task Should_apply_duplicates_of_a_key_in_order_across_batches()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions { BatchSize = 1 });

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(2);
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

      // The primary key collides and the key column (item_count) matches, so the row is updated; the second row has a different key and is skipped.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Changed", Count = 1 },
                                                              new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Skipped", Count = 2 }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions
                                                           {
                                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithDifferentColumnNames>(e => e.Count)
                                                           });

      affectedRows.Should().Be(2);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDifferentColumnNames.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.Name.Should().Be("Changed");
      loadedEntity.Count.Should().Be(1);
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
   public async Task Should_throw_if_the_auto_increment_key_is_not_selected_for_insert()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name";
      var newEntity = new TestEntityWithAutoIncrement { Name = "New Entity" };

      // MySQL needs the key in the INSERT, otherwise it can't tell whether a row exists.
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                            new MySqlBulkInsertOrUpdateOptions
                                                            {
                                                               PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name),
                                                               PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name)
                                                            }))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("MySQL upsert requires key property 'Id' to be selected for insertion.");

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.SingleAsync();
      loadedEntity.Name.Should().Be("original value");
   }

   [Fact]
   public async Task Should_throw_if_an_update_property_is_not_selected_for_insert()
   {
      var existingEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Count = 42;

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([existingEntity],
                                                            new MySqlBulkInsertOrUpdateOptions
                                                            {
                                                               PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => new { e.Id, e.RequiredName }),
                                                               PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                                            }))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("MySQL upsert requires update property 'Count' to also be selected for insertion.");

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_if_a_key_property_is_not_selected_for_insert()
   {
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }],
                                                            new MySqlBulkInsertOrUpdateOptions
                                                            {
                                                               PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => new { e.Id, e.RequiredName }),
                                                               PropertiesToUpdate = IEntityPropertiesProvider.Empty,
                                                               KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name)
                                                            }))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("MySQL upsert requires key property 'Name' to be selected for insertion.");

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
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

      affectedRows.Should().Be(2);

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
   public async Task Should_skip_rows_that_collide_with_another_row_on_a_unique_index()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "a" }, new MySqlTestEntity { Id = 2, Name = "b" });
      await ArrangeDbContext.SaveChangesAsync();

      // The name of MySqlTestEntity has a unique index. The first row collides with row 1 on that index, but its primary key is new.
      var affectedRows = await SUT.BulkInsertOrUpdateAsync([
                                                              new MySqlTestEntity { Id = 9, Name = "a" },
                                                              new MySqlTestEntity { Id = 2, Name = "b changed" },
                                                              new MySqlTestEntity { Id = 10, Name = "new" }
                                                           ],
                                                           new MySqlBulkInsertOrUpdateOptions { BatchSize = 1 });

      affectedRows.Should().Be(3, "skipped rows are counted as processed");

      var loadedEntities = await AssertDbContext.Entities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name)).Should().Equal((1, "a"), (2, "b changed"), (10, "new"));
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
   public async Task Should_insert_or_update_entities_in_table_of_the_database_from_ForRootEntity_provider()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database.
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
   public async Task Should_upsert_inlined_owned_type()
   {
      var existingEntity = new TestEntity_Owns_Inline { Id = _TEST_ENTITY_ID, InlineEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } };
      ArrangeDbContext.TestEntities_Own_Inline.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.InlineEntity.IntColumn = 2;
      existingEntity.InlineEntity.StringColumn = "new";
      var newEntity = new TestEntity_Owns_Inline { Id = _OTHER_TEST_ENTITY_ID, InlineEntity = new OwnedEntity { IntColumn = 3, StringColumn = null } };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.OrderBy(e => e.InlineEntity.IntColumn).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity], config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_upsert_nested_inlined_owned_type()
   {
      var existingEntity = new TestEntity_Owns_Inline_Inline
                           {
                              Id = _TEST_ENTITY_ID,
                              InlineEntity = new OwnedEntity_Owns_Inline { IntColumn = 1, StringColumn = "old", InlineEntity = new OwnedEntity { IntColumn = 10, StringColumn = "old inner" } }
                           };
      ArrangeDbContext.TestEntities_Own_Inline_Inline.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.InlineEntity.IntColumn = 2;
      existingEntity.InlineEntity.InlineEntity.StringColumn = "new inner";
      var newEntity = new TestEntity_Owns_Inline_Inline
                      {
                         Id = _OTHER_TEST_ENTITY_ID,
                         InlineEntity = new OwnedEntity_Owns_Inline { IntColumn = 3, StringColumn = "new", InlineEntity = new OwnedEntity { IntColumn = 30, StringColumn = null } }
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_Inline.OrderBy(e => e.InlineEntity.IntColumn).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity], config => config.WithStrictOrdering());
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

   // The tests below cover behavior specific to the MySQL executor: batching, transactions, command timeout, syntax of old server versions.

   [Fact]
   public async Task Should_not_use_a_temp_table()
   {
      await SUT.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }], new MySqlBulkInsertOrUpdateOptions());

      ExecutedCommands.Should().NotContain(command => command.Contains("TEMPORARY", StringComparison.Ordinal));
   }

   [Theory]
   [InlineData(1, null, 1)]
   [InlineData(499, null, 1)]
   [InlineData(500, null, 1)] // the default batch size
   [InlineData(501, null, 2)]
   [InlineData(1001, null, 3)]
   [InlineData(7, 1, 7)]
   [InlineData(7, 2, 4)]
   [InlineData(7, 100, 1)]
   public async Task Should_split_rows_into_statements_according_to_batch_size(int rowCount, int? batchSize, int expectedStatements)
   {
      // every second row exists already, so each batch inserts and updates rows
      ArrangeDbContext.TestEntitiesWithAutoIncrement.AddRange(Enumerable.Range(1, rowCount).Where(id => id % 2 == 0).Select(id => new TestEntityWithAutoIncrement { Id = id, Name = $"old {id}" }));
      await ArrangeDbContext.SaveChangesAsync();

      var entities = Enumerable.Range(1, rowCount).Select(id => new TestEntityWithAutoIncrement { Id = id, Name = $"new {id}" }).ToList();

      // The status counters are per session. The connection stays open, so the executor and the status queries share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions { BatchSize = batchSize });
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
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions { BatchSize = Int32.MaxValue });
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
   public async Task Should_throw_if_batch_size_is_not_positive(int batchSize)
   {
      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }], new MySqlBulkInsertOrUpdateOptions { BatchSize = batchSize }))
               .Should().ThrowAsync<ArgumentOutOfRangeException>()
               .WithMessage("*batch size*greater than 0*");

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
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
                                                  new MySqlBulkInsertOrUpdateOptions { BatchSize = 1 });

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

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 }], new MySqlBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);
      }

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.Count.Should().Be(0);
   }

   [Theory]
   [InlineData(false, "8.4.0", false)]
   [InlineData(false, "8.0.19", false)]
   [InlineData(false, "8.0.18", true)]
   [InlineData(false, "5.7.44", true)]
   [InlineData(true, "10.11.0", true)]
   public async Task Should_use_the_syntax_of_the_configured_server_version(bool isMariaDb, string version, bool expectValuesFunction)
   {
      await using var context = CreateContext(isMariaDb ? new MariaDbServerVersion(Version.Parse(version)) : new MySqlServerVersion(Version.Parse(version)));
      context.Entities.Add(new MySqlTestEntity { Id = 1, Name = "old" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();
      await using var transaction = await context.Database.BeginTransactionAsync();

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "new" }, new MySqlTestEntity { Id = 2, Name = "added" }],
                                                                                                         new MySqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      // The deprecated VALUES(col) function causes warning 1287 on MySQL 8.4; the row alias doesn't.
      var warnings = await GetWarningsAsync(context);
      if (expectValuesFunction)
         warnings.Should().Contain(warning => warning.Code == 1287);
      else
         warnings.Should().NotContain(warning => warning.Code == 1287);

      await transaction.CommitAsync();

      var loadedEntities = await AssertDbContext.Entities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name)).Should().Equal((1, "new"), (2, "added"));
   }

   [Fact]
   public async Task Should_guard_the_update_with_the_key_columns_on_server_versions_without_row_alias()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "value", RequiredName = "RequiredName", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();
      await using var context = CreateContext(_SERVER_VERSION_WITHOUT_ROW_ALIAS);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync([
                                                                                                           new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName", Count = 10 },
                                                                                                           new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "other value", RequiredName = "RequiredName", Count = 20 },
                                                                                                           new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "new", RequiredName = "RequiredName", Count = 30 }
                                                                                                        ],
                                                                                                        new MySqlBulkInsertOrUpdateOptions
                                                                                                        {
                                                                                                           KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name)
                                                                                                        });

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_OTHER_TEST_ENTITY_ID, "value", 2),
                                                                         (_TEST_ENTITY_ID, (string?)null, 10),
                                                                         (_THIRD_TEST_ENTITY_ID, "new", 30));
   }

   [Fact]
   public async Task Should_skip_rows_that_collide_on_a_unique_index_on_server_versions_without_row_alias()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "a" }, new MySqlTestEntity { Id = 2, Name = "b" });
      await ArrangeDbContext.SaveChangesAsync();
      await using var context = CreateContext(_SERVER_VERSION_WITHOUT_ROW_ALIAS);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync([
                                                                                                           new MySqlTestEntity { Id = 9, Name = "a" },
                                                                                                           new MySqlTestEntity { Id = 2, Name = "b changed" },
                                                                                                           new MySqlTestEntity { Id = 10, Name = "new" }
                                                                                                        ],
                                                                                                        new MySqlBulkInsertOrUpdateOptions { BatchSize = 2 });

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.Entities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name)).Should().Equal((1, "a"), (2, "b changed"), (10, "new"));
   }

   [Fact]
   public async Task Should_upsert_entities_with_composite_primary_key_on_server_versions_without_row_alias()
   {
      ArrangeDbContext.CompositeEntities.AddRange(new MySqlCompositeEntity { RegionId = 1, ItemId = 1, Name = "1-1" },
                                                  new MySqlCompositeEntity { RegionId = 1, ItemId = 2, Name = "1-2" });
      await ArrangeDbContext.SaveChangesAsync();
      await using var context = CreateContext(_SERVER_VERSION_WITHOUT_ROW_ALIAS);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync([
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
   public async Task Should_only_insert_new_rows_if_no_property_is_left_to_update_on_server_versions_without_row_alias()
   {
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 1, Name = "a" });
      await ArrangeDbContext.SaveChangesAsync();
      await using var context = CreateContext(_SERVER_VERSION_WITHOUT_ROW_ALIAS);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "changed" }, new MySqlTestEntity { Id = 2, Name = "new" }],
                                                                                                        new MySqlBulkInsertOrUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Empty });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.Entities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name)).Should().Equal((1, "a"), (2, "new"));
   }

   private MySqlTestDbContext CreateContext(ServerVersion serverVersion)
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql(ConnectionString, serverVersion, builder => builder.AddBulkOperationSupport())
                    .Options;

      return new MySqlTestDbContext(options);
   }

   // The warnings belong to the last statement of the session, so the connection has to be the one of the executor and no statement may run in between.
   private static async Task<List<(string Level, int Code, string Message)>> GetWarningsAsync(DbContext context)
   {
      await using var command = context.Database.GetDbConnection().CreateCommand();
      command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
      command.CommandText = "SHOW WARNINGS";
      await using var reader = await command.ExecuteReaderAsync();

      var warnings = new List<(string Level, int Code, string Message)>();

      while (await reader.ReadAsync())
      {
         warnings.Add((reader.GetString(0), Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture), reader.GetString(2)));
      }

      return warnings;
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
