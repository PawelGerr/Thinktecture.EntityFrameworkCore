using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertOrUpdateAsync : IntegrationTestsBase
{
   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public BulkInsertOrUpdateAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_entities_is_empty()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_new_entities()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var entity = new TestEntity
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1)
                    .And.Subject.First()
                    .Should().BeEquivalentTo(new TestEntity
                                             {
                                                Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                Name = "Name",
                                                RequiredName = "RequiredName",
                                                Count = 42
                                             });
   }

   [Fact]
   public async Task Should_update_existing_entities()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name";
      entity.Count = 42;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1)
                    .And.Subject.First()
                    .Should().BeEquivalentTo(entity);
   }

   [Fact]
   public async Task Should_insert_and_update_in_same_batch()
   {
      var existingEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "UpdatedName";
      existingEntity.Count = 1;

      var newEntity = new TestEntity
                      {
                         Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                         Name = "NewName",
                         RequiredName = "RequiredName",
                         Count = 2
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity]);
   }

   [Fact]
   public async Task Should_throw_when_entity_has_no_key()
   {
      await SUT.Invoking(sut => sut.BulkInsertOrUpdateAsync(new List<KeylessTestEntity> { new() }, new NpgsqlBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.KeylessTestEntity' has no primary key. Please provide key properties to perform JOIN/match on.");
   }

   [Fact]
   public async Task Should_insert_column_with_converter()
   {
      var existingEntity = new TestEntity { Id = new Guid("79DA4171-C90B-4A5D-B0B5-D0A1E1BDF966"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.ConvertibleClass = new ConvertibleClass(43);
      var newEntity = new TestEntity
                      {
                         Id = new Guid("3DAEA618-B732-4BCA-A5A1-D1E075022DEC"),
                         RequiredName = "RequiredName",
                         ConvertibleClass = new ConvertibleClass(42)
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var entities = AssertDbContext.TestEntities.ToList();
      entities.Should().BeEquivalentTo([
         new TestEntity { Id = new Guid("79DA4171-C90B-4A5D-B0B5-D0A1E1BDF966"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(43) },
                                          new TestEntity { Id = new Guid("3DAEA618-B732-4BCA-A5A1-D1E075022DEC"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(42) },
      ]);
   }

   [Fact]
   public async Task Should_insert_private_property()
   {
      var existingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.SetPrivateField(1);

      var newEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      newEntity.SetPrivateField(3);

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([newEntity, existingEntity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var expectedExistingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC"), RequiredName = "RequiredName" };
      expectedExistingEntity.SetPrivateField(1);

      var expectedNewEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      expectedNewEntity.SetPrivateField(3);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([expectedNewEntity, expectedExistingEntity]);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      var existingEntity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      ActDbContext.Entry(existingEntity).Property("ShadowStringProperty").CurrentValue = "value1";
      ActDbContext.Entry(existingEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var newEntity = new TestEntityWithShadowProperties { Id = new Guid("884BB11B-088D-4BA5-B75D-C7F36B88378B") };
      ActDbContext.Entry(newEntity).Property("ShadowStringProperty").CurrentValue = "value2";
      ActDbContext.Entry(newEntity).Property("ShadowIntProperty").CurrentValue = 43;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([newEntity, existingEntity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithShadowProperties.ToListAsync();
      loadedEntities.Should().HaveCount(2);

      var existingEntry = AssertDbContext.Entry(loadedEntities.Single(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866")));
      existingEntry.Property("ShadowStringProperty").CurrentValue.Should().Be("value1");
      existingEntry.Property("ShadowIntProperty").CurrentValue.Should().Be(42);

      var newEntry = AssertDbContext.Entry(loadedEntities.Single(e => e.Id == new Guid("884BB11B-088D-4BA5-B75D-C7F36B88378B")));
      newEntry.Property("ShadowStringProperty").CurrentValue.Should().Be("value2");
      newEntry.Property("ShadowIntProperty").CurrentValue.Should().Be(43);
   }

   [Fact]
   public async Task Should_insert_specified_properties_only()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();

      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42,
                          PropertyWithBackingField = 7
                       };
      testEntity.SetPrivateField(3);
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertOrUpdateAsync(testEntities,
                                        new NpgsqlBulkInsertOrUpdateOptions
                                        {
                                           PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                        });

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                              RequiredName = "RequiredName",
                                              Count = 42,
                                              PropertyWithBackingField = 7
                                           });
      loadedEntity.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_not_throw_if_key_property_is_not_present_in_PropertiesToUpdate()
   {
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => new { entity.Name });

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new NpgsqlBulkInsertOrUpdateOptions
                                                                                   {
                                                                                      PropertiesToUpdate = propertiesProvider
                                                                                   });

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_entities()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = "Name";
      entity_1.Count = 1;
      entity_2.Name = "OtherName";
      entity_2.Count = 2;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity_1, entity_2], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_update_provided_entity_only()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = "Name";
      entity_1.Count = 1;
      entity_2.Name = "OtherName";
      entity_2.Count = 2;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity_1], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      entity_2.Name = default;
      entity_2.Count = default;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_update_specified_properties_only()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name"; // this would not be updated
      entity.Count = 42;
      entity.PropertyWithBackingField = 7;
      entity.SetPrivateField(3);

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity],
                                                           new NpgsqlBulkInsertOrUpdateOptions
                                                           {
                                                              PropertiesToUpdate = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                                           });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                              Count = 42,
                                              PropertyWithBackingField = 7,
                                              Name = "original value",
                                              RequiredName = "RequiredName"
                                           });
      loadedEntity.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_and_update_TestEntity_with_ComplexType()
   {
      var testEntity_1 = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                       new BoundaryValueObject(2, 5));

      ArrangeDbContext.Add(testEntity_1);
      await ArrangeDbContext.SaveChangesAsync();

      testEntity_1.Boundary = new BoundaryValueObject(10, 20);
      var testEntity_2 = new TestEntityWithComplexType(new Guid("67A9500B-CF51-4A39-8C89-F2EBF7EDE84D"),
                                                       new BoundaryValueObject(3, 4));

      await SUT.BulkInsertOrUpdateAsync([testEntity_1, testEntity_2], new NpgsqlBulkInsertOrUpdateOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity_1, testEntity_2]);
   }

   [Fact]
   public async Task Should_insert_new_entities_with_ConflictDoNothing()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity], new NpgsqlBulkInsertOrUpdateOptions { ConflictDoNothing = true });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1)
                    .And.Subject.First()
                    .Should().BeEquivalentTo(entity);
   }

   [Fact]
   public async Task Should_skip_conflicting_rows_with_ConflictDoNothing()
   {
      var existingEntity = new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Original",
                              RequiredName = "RequiredName",
                              Count = 1
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var conflictingEntity = new TestEntity
                              {
                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                 Name = "Updated",
                                 RequiredName = "UpdatedRequiredName",
                                 Count = 99
                              };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(
                                                           [conflictingEntity],
         new NpgsqlBulkInsertOrUpdateOptions { ConflictDoNothing = true });

      affectedRows.Should().Be(0);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(existingEntity);
   }

   [Fact]
   public async Task Should_insert_new_and_skip_conflicting_with_ConflictDoNothing()
   {
      var existingEntity = new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Original",
                              RequiredName = "RequiredName",
                              Count = 1
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var conflictingEntity = new TestEntity
                              {
                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                 Name = "Updated",
                                 RequiredName = "UpdatedRequiredName",
                                 Count = 99
                              };
      var newEntity = new TestEntity
                      {
                         Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                         Name = "NewName",
                         RequiredName = "RequiredName",
                         Count = 2
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(
                                                           [conflictingEntity, newEntity],
         new NpgsqlBulkInsertOrUpdateOptions { ConflictDoNothing = true });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existingEntity, newEntity]);
   }

   [Fact]
   public async Task Should_insert_or_update_entities_in_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_BulkUpsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         INSERT INTO "{Schema}"."TestEntities_BulkUpsertRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
         VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 'RequiredName', 'OldName', 0, NULL, NULL, NULL, 0, 0);
         """);

      try
      {
         var existingEntity = new TestEntity
                              {
                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                 RequiredName = "RequiredName",
                                 Name = "UpdatedName",
                                 Count = 99
                              };
         var newEntity = new TestEntity
                         {
                            Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                            RequiredName = "RequiredName",
                            Name = "NewName",
                            Count = 1
                         };

         var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity],
                                                              new NpgsqlBulkInsertOrUpdateOptions { TableName = "TestEntities_BulkUpsertRedirect", Schema = Schema });

         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_BulkUpsertRedirect" ORDER BY "Name" """)
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(2);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_BulkUpsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_or_update_entity_with_jsonb_and_json_columns()
   {
      var existingEntity = new TestEntityWithJsonColumns
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              JsonbColumn = """{"key": "original"}""",
                              JsonColumn = """{"key": "original"}"""
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.JsonbColumn = """{"key": "updated"}""";
      existingEntity.JsonColumn = """{"key": "updated"}""";

      var newEntity = new TestEntityWithJsonColumns
                      {
                         Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                         JsonbColumn = """{"key": "new"}""",
                         JsonColumn = """{"key": "new"}"""
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([existingEntity, newEntity], new NpgsqlBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithJsonColumns.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Should().HaveCount(2);

      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[0].JsonbColumn.Should().Be("""{"key": "updated"}""");
      loadedEntities[0].JsonColumn.Should().Be("""{"key": "updated"}""");

      loadedEntities[1].Id.Should().Be(new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loadedEntities[1].JsonbColumn.Should().Be("""{"key": "new"}""");
      loadedEntities[1].JsonColumn.Should().Be("""{"key": "new"}""");
   }
}
