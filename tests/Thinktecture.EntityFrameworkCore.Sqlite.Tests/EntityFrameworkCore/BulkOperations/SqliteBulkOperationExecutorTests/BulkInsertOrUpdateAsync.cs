using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertOrUpdateAsync : IntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;

   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkInsertOrUpdateAsync(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
      : base(testOutputHelper, providerFactoryFixture)
   {
   }

   [Fact]
   public async Task Should_throw_when_entity_has_no_key()
   {
      await SUT.Invoking(sut => sut.BulkInsertOrUpdateAsync(new List<KeylessTestEntity> { new() }, new SqliteBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.KeylessTestEntity' has no primary key. Please provide key properties to perform JOIN/match on.");
   }

   [Fact]
   public async Task Should_not_throw_if_entities_is_empty()
   {
      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(0);
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity }, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var entities = AssertDbContext.TestEntities.ToList();
      entities.Should().BeEquivalentTo(new[]
                                       {
                                          new TestEntity { Id = new Guid("79DA4171-C90B-4A5D-B0B5-D0A1E1BDF966"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(43) },
                                          new TestEntity { Id = new Guid("3DAEA618-B732-4BCA-A5A1-D1E075022DEC"), RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(42) }
                                       });
   }

   [Fact]
   public async Task Should_insert_entities()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };

      await SUT.BulkInsertOrUpdateAsync(new[] { testEntity }, new SqliteBulkInsertOrUpdateOptions());

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
   public async Task Should_insert_private_property()
   {
      var existingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.SetPrivateField(1);

      var newEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      newEntity.SetPrivateField(3);

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { newEntity, existingEntity }, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var expectedExistingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC"), RequiredName = "RequiredName" };
      expectedExistingEntity.SetPrivateField(1);

      var expectedNewEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      expectedNewEntity.SetPrivateField(3);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { expectedNewEntity, expectedExistingEntity });
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { newEntity, existingEntity }, new SqliteBulkInsertOrUpdateOptions());

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
   public async Task Should_insert_and_update_entities_with_auto_increment_column_having_property_selector()
   {
      var existingEntity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      existingEntity.Name = "Name";

      var newEntity = new TestEntityWithAutoIncrement { Name = "New Entity" };
      var testEntities = new[] { existingEntity, newEntity };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(testEntities, new SqliteBulkInsertOrUpdateOptions
                                                                         {
                                                                            PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(entity => entity.Name)
                                                                         });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities.Should().AllSatisfy(e => e.Id.Should().NotBe(0));
      loadedEntities.Should().BeEquivalentTo(new[]
                                             {
                                                existingEntity,
                                                new TestEntityWithAutoIncrement
                                                {
                                                   Id = loadedEntities.Single(e => e.Id != existingEntity.Id).Id,
                                                   Name = "New Entity"
                                                }
                                             });
   }

   [Fact]
   public async Task Should_insert_specified_properties_only()
   {
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
                                        new SqliteBulkInsertOrUpdateOptions
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new SqliteBulkInsertOrUpdateOptions
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { entity_1, entity_2 });
   }

   [Fact]
   public async Task Should_update_entities_based_on_non_pk_property()
   {
      var keyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name);

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync(new TestEntity[0], new SqliteBulkInsertOrUpdateOptions { KeyProperties = keyProperties }))
               .Should().ThrowAsync<InvalidOperationException>().WithMessage("Error during bulk operation on table '\"TestEntities\"'. See inner exception for more details.")
               .WithInnerException<InvalidOperationException, SqliteException>().WithMessage("SQLite Error 1: 'ON CONFLICT clause does not match any PRIMARY KEY or UNIQUE constraint'.");
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity_1 }, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      entity_2.Name = default;
      entity_2.Count = default;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { entity_1, entity_2 });
   }

   [Fact]
   public async Task Should_write_not_nullable_structs_as_is_despite_sql_default_value()
   {
      var entity = new TestEntityWithSqlDefaultValues
                   {
                      Id = new Guid("53A5EC9B-BB8D-4B9D-9136-68C011934B63"),
                      Int = 1,
                      String = "value",
                      NullableInt = 2,
                      NullableString = "otherValue"
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Int = 0;
      entity.String = "other value";
      entity.NullableInt = null;
      entity.NullableString = null;

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity }, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.FirstOrDefaultAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                           {
                                              Id = new Guid("53A5EC9B-BB8D-4B9D-9136-68C011934B63"),
                                              Int = 0,            // persisted as-is
                                              NullableInt = null, // persisted as-is
                                              String = "other value",
                                              NullableString = null // persisted as-is
                                           });
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity },
                                                           new SqliteBulkInsertOrUpdateOptions
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
   public async Task Should_skip_update_if_no_properties_to_update()
   {
      var existingEntity = new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var newEntity = new TestEntity
                      {
                         Id = new Guid("3AA6D70D-C619-4EB5-9819-8030506EA637"),
                         Name = "new",
                         RequiredName = "RequiredName",
                         Count = 1
                      };

      existingEntity.Name = "changed";
      existingEntity.Count = 43;

      var affectedRows = await ActDbContext.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity },
                                                                    new SqliteBulkInsertOrUpdateOptions
                                                                    {
                                                                       PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties()),
                                                                       PropertiesToUpdate = IEntityPropertiesProvider.Empty
                                                                    }
                                                                   );

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[]
                                             {
                                                new TestEntity
                                                {
                                                   Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                   Name = "Name",
                                                   RequiredName = "RequiredName",
                                                   Count = 42
                                                },
                                                new TestEntity
                                                {
                                                   Id = new Guid("3AA6D70D-C619-4EB5-9819-8030506EA637"),
                                                   Name = null, // is not a required property
                                                   RequiredName = "RequiredName",
                                                   Count = 1
                                                }
                                             });
   }

   [Fact]
   public async Task Should_skip_update_if_provided_key_properties_only()
   {
      var existingEntity = new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var newEntity = new TestEntity
                      {
                         Id = new Guid("3AA6D70D-C619-4EB5-9819-8030506EA637"),
                         Name = "new",
                         RequiredName = "RequiredName",
                         Count = 1
                      };

      existingEntity.Name = "changed";
      existingEntity.Count = 43;

      var affectedRows = await ActDbContext.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity },
                                                                    new SqliteBulkInsertOrUpdateOptions
                                                                    {
                                                                       PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties()),
                                                                       PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(entity => entity.Id)
                                                                    }
                                                                   );

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[]
                                             {
                                                new TestEntity
                                                {
                                                   Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                   Name = "Name",
                                                   RequiredName = "RequiredName",
                                                   Count = 42
                                                },
                                                new TestEntity
                                                {
                                                   Id = new Guid("3AA6D70D-C619-4EB5-9819-8030506EA637"),
                                                   Name = null, // is not a required property
                                                   RequiredName = "RequiredName",
                                                   Count = 1
                                                }
                                             });
   }

   [Fact]
   public async Task Should_insert_and_update_TestEntity_with_ComplexType()
   {
      // Arrange
      var testEntity_1 = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                       new BoundaryValueObject(2, 5));

      ArrangeDbContext.Add(testEntity_1);
      await ArrangeDbContext.SaveChangesAsync();

      // Act
      testEntity_1.Boundary = new BoundaryValueObject(10, 20);
      var testEntity_2 = new TestEntityWithComplexType(new Guid("67A9500B-CF51-4A39-8C89-F2EBF7EDE84D"),
                                                       new BoundaryValueObject(3, 4));

      await SUT.BulkInsertOrUpdateAsync(new[] { testEntity_1, testEntity_2 }, new SqliteBulkInsertOrUpdateOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity_1, testEntity_2 });
   }

   [Fact]
   public async Task Should_insert_or_update_entities_in_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_BulkUpsertRedirect" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "Name" TEXT NULL,
            "RequiredName" TEXT NOT NULL,
            "Count" INTEGER NOT NULL,
            "NullableCount" INTEGER NULL,
            "ConvertibleClass" INTEGER NULL,
            "ParentId" TEXT NULL,
            "PropertyWithBackingField" INTEGER NOT NULL,
            "_privateField" INTEGER NOT NULL
         );
         INSERT INTO "TestEntities_BulkUpsertRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
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

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity },
                                                               new SqliteBulkInsertOrUpdateOptions { TableName = "TestEntities_BulkUpsertRedirect" });

         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<string>("SELECT [Id] FROM [TestEntities_BulkUpsertRedirect] ORDER BY [Name]")
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(2);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_BulkUpsertRedirect]");
      }
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_upsert_entities_in_table_from_table_name_provider(bool useGenericOptions)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_BulkUpsertRedirect" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "Name" TEXT NULL,
            "RequiredName" TEXT NOT NULL,
            "Count" INTEGER NOT NULL,
            "NullableCount" INTEGER NULL,
            "ConvertibleClass" INTEGER NULL,
            "ParentId" TEXT NULL,
            "PropertyWithBackingField" INTEGER NOT NULL,
            "_privateField" INTEGER NOT NULL
         );
         INSERT INTO "TestEntities_BulkUpsertRedirect" ("Id", "RequiredName", "Name", "Count", "NullableCount", "ConvertibleClass", "ParentId", "PropertyWithBackingField", "_privateField")
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
         var provider = new SuffixTableNameProvider("_BulkUpsertRedirect");

         IBulkInsertOrUpdateOptions options = useGenericOptions
                                                 ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                                 : new SqliteBulkInsertOrUpdateOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity }, options);

         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>("SELECT [Name] FROM [TestEntities_BulkUpsertRedirect] ORDER BY [Name]")
                                                   .ToListAsync();
         redirectedNames.Should().BeEquivalentTo(["NewName", "UpdatedName"]);
         provider.RequestedEntityTypes.Should().ContainSingle();
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_BulkUpsertRedirect]");
      }
   }

   [Fact]
   public async Task Should_upsert_inlined_owned_type()
   {
      ArrangeDbContext.TestEntities_Own_Inline.Add(new TestEntity_Owns_Inline { Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), InlineEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_Inline.SingleAsync();
      entity.InlineEntity.IntColumn = 2;
      entity.InlineEntity.StringColumn = "new";
      var newEntity = new TestEntity_Owns_Inline { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), InlineEntity = new OwnedEntity { IntColumn = 3, StringColumn = "inserted" } };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync([entity, newEntity], new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities_Own_Inline.ToListAsync()).Select(e => e.InlineEntity)
                                                                  .Should().BeEquivalentTo([new OwnedEntity { IntColumn = 2, StringColumn = "new" }, new OwnedEntity { IntColumn = 3, StringColumn = "inserted" }]);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_owned_type_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateOne.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"), SeparateEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateOne.SingleAsync();
      entity.SeparateEntity!.IntColumn = 2;
      var newEntity = new TestEntity_Owns_SeparateOne { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), SeparateEntity = new OwnedEntity { IntColumn = 3 } };
      ActDbContext.Add(newEntity);

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity, newEntity], new SqliteBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("The owned type property '*' is selected for bulk-insert-or-update but there are no properties for performing the update.");

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().ContainSingle();
      loadedEntities[0].SeparateEntity!.IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_owned_collection_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateMany.Add(new TestEntity_Owns_SeparateMany { Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"), SeparateEntities = [new() { IntColumn = 1, StringColumn = "old" }] });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateMany.SingleAsync();
      entity.SeparateEntities[0].IntColumn = 2;
      var newEntity = new TestEntity_Owns_SeparateMany { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), SeparateEntities = [new() { IntColumn = 3 }] };
      ActDbContext.Add(newEntity);

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity, newEntity], new SqliteBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("The owned type property '*' is selected for bulk-insert-or-update but there are no properties for performing the update.");

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync();
      loadedEntities.Should().ContainSingle();
      loadedEntities[0].SeparateEntities.Single().IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_upsert_of_owned_type_in_separate_table_before_writing_root_table()
   {
      var entity = new TestEntity_Owns_SeparateOne { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), SeparateEntity = new OwnedEntity { IntColumn = 3 } };
      ActDbContext.Add(entity);

      await using var tx = await ActDbContext.Database.BeginTransactionAsync();

      await SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync([entity], new SqliteBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("The owned type property '*' is selected for bulk-insert-or-update but there are no properties for performing the update.");

      // The caller transaction is still open, so a root row written before the exception would be visible here.
      (await ActDbContext.Database.SqlQueryRaw<int>("""SELECT COUNT(*) AS "Value" FROM "TestEntities_Own_SeparateOne" """).SingleAsync()).Should().Be(0);
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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new SqliteBulkInsertOrUpdateOptions());

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

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(entities, new SqliteBulkInsertOrUpdateOptions());

      affectedRows.Should().Be(4);
      var loaded = await AssertDbContext.TestEntitiesWithStructsInOptionalComplexType.ToListAsync();
      loaded.Should().BeEquivalentTo(entities);

      var structColumns = await TestEntityWithStructsInOptionalComplexType.QueryStructColumns(AssertDbContext, "\"TestEntitiesWithStructsInOptionalComplexType\"").ToListAsync();
      structColumns.Should().BeEquivalentTo(entities.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
   }
}
