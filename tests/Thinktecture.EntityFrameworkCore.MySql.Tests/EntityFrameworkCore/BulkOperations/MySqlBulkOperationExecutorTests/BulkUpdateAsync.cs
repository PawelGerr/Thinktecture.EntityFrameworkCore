using System.Diagnostics;
using System.Globalization;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// The executor stages the rows in a temp table (EF creates it, so the CREATE statement is visible in ExecutedCommands) and runs one raw
// "UPDATE target INNER JOIN staging" statement. That statement is not visible in ExecutedCommands, so the tests verify the resulting rows
// and the return values (the number of matched rows, i.e. rows with unchanged values are counted as well).
// Inside a System.Transactions.TransactionScope the executor updates row by row because MySQL rejects temp tables inside XA transactions.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkUpdateAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("5B9587A3-2312-43DF-9681-38EC22AD8606");
   private static readonly Guid _MISSING_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkUpdateAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_entities_is_empty()
   {
      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity>(), new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_not_throw_if_key_property_is_not_present_in_PropertiesToUpdate()
   {
      var propertiesProvider = IEntityPropertiesProvider.Include<TestEntity>(entity => new { entity.Name });

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity>(), new MySqlBulkUpdateOptions { PropertiesToUpdate = propertiesProvider });

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_when_entity_has_no_key()
   {
      await SUT.Awaiting(sut => sut.BulkUpdateAsync(new List<KeylessTestEntity> { new() }, new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.KeylessTestEntity' has no primary key. Please provide key properties to perform JOIN/match on.");
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

      var affectedRows = await SUT.BulkUpdateAsync([entity_1, entity_2], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_return_0_if_no_rows_match()
   {
      var entity = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity> { new() { Id = _MISSING_TEST_ENTITY_ID, RequiredName = "Other", Count = 42 } }, new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(0);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(entity);
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

      var affectedRows = await SUT.BulkUpdateAsync([entity_1], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      entity_2.Name = default;
      entity_2.Count = default;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_count_matched_rows_whose_values_do_not_change()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "OtherName", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      // MySqlConnector requests the number of found rows instead of the number of changed rows.
      var affectedRows = await SUT.BulkUpdateAsync([entity_1, entity_2], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_update_the_existing_rows_of_the_input_only()
   {
      var existing = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1 };
      var untouched = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Untouched", RequiredName = "RequiredName", Count = 2 };
      ArrangeDbContext.AddRange(existing, untouched);
      await ArrangeDbContext.SaveChangesAsync();

      existing.Name = "Changed";
      existing.Count = 10;
      var missing = new TestEntity { Id = _MISSING_TEST_ENTITY_ID, Name = "Missing", RequiredName = "RequiredName", Count = 3 };

      var affectedRows = await SUT.BulkUpdateAsync([existing, missing], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([existing, untouched]);
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

      var affectedRows = await SUT.BulkUpdateAsync([entity],
                                                   new MySqlBulkUpdateOptions
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
   public async Task Should_stage_key_and_update_properties_only_in_the_temp_table()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name";

      await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name) });

      var createStatement = ExecutedCommands.Should().ContainSingle(command => command.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal)).Subject;
      createStatement.Should().Contain("`Id`").And.Contain("`Name`");
      createStatement.Should().NotContain("`RequiredName`").And.NotContain("`Count`");
   }

   [Fact]
   public async Task Should_update_entities_based_on_non_pk_property()
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, Name = "value", RequiredName = "RequiredName" };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = null, RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = null;
      entity_1.Count = 1;
      entity_2.Name = "value";
      entity_2.Count = 2;

      var properties = IEntityPropertiesProvider.Include<TestEntity>(e => new { e.Count });
      var keyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name);
      var affectedRows = await SUT.BulkUpdateAsync([entity_1, entity_2], new MySqlBulkUpdateOptions { PropertiesToUpdate = properties, KeyProperties = keyProperties });

      affectedRows.Should().Be(2);

      // The key is the name, so the counts are swapped between the rows.
      entity_1.Name = "value";
      entity_2.Name = null;

      entity_1.Count = 2;
      entity_2.Count = 1;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_1, entity_2]);
   }

   [Fact]
   public async Task Should_not_update_key_columns_that_are_selected_for_update()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName", Count = 1 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // The row is matched by Name; Name is selected for update as well but is never part of the SET clause.
      var affectedRows = await SUT.BulkUpdateAsync([new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "original value", RequiredName = "Other", Count = 42 }],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => new { e.Name, e.Count })
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName", Count = 42 });
   }

   [Fact]
   public async Task Should_update_entities_based_on_composite_non_pk_properties()
   {
      ArrangeDbContext.TestEntitiesWithFlags.AddRange(new TestEntityWithFlags { Id = _TEST_ENTITY_ID, GroupId = 1, Phase = PhaseMembership.A },
                                                      new TestEntityWithFlags { Id = _OTHER_TEST_ENTITY_ID, GroupId = 1, Phase = PhaseMembership.B },
                                                      new TestEntityWithFlags { Id = _THIRD_TEST_ENTITY_ID, GroupId = 2, Phase = PhaseMembership.B });
      await ArrangeDbContext.SaveChangesAsync();

      // Only the row with GroupId = 1 and Phase = B matches both key columns.
      var affectedRows = await SUT.BulkUpdateAsync([new TestEntityWithFlags { Id = _MISSING_TEST_ENTITY_ID, GroupId = 1, Phase = PhaseMembership.B }],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithFlags>(e => new { e.GroupId, e.Phase }),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithFlags>(e => e.Id)
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntitiesWithFlags.OrderBy(e => e.GroupId).ThenBy(e => e.Phase).ToListAsync();
      loadedEntities.Should().HaveCount(3);
      loadedEntities.Select(e => (e.Id, e.GroupId, e.Phase)).Should().Equal((_TEST_ENTITY_ID, 1, PhaseMembership.A),
                                                                            (_MISSING_TEST_ENTITY_ID, 1, PhaseMembership.B),
                                                                            (_THIRD_TEST_ENTITY_ID, 2, PhaseMembership.B));
   }

   [Fact]
   public async Task Should_match_keys_with_the_collation_of_the_column()
   {
      ArrangeDbContext.TestEntitiesWithCollation.AddRange(new TestEntityWithCollation(_TEST_ENTITY_ID, "old 1", "abc"),
                                                          new TestEntityWithCollation(_OTHER_TEST_ENTITY_ID, "old 2", "ABC"));
      await ArrangeDbContext.SaveChangesAsync();

      // The column is case-sensitive (utf8mb4_bin), so "ABC" must not match "abc". Without the collation in the staging table the JOIN fails with error 1267.
      var affectedRows = await SUT.BulkUpdateAsync([new TestEntityWithCollation(_MISSING_TEST_ENTITY_ID, "new", "ABC")],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithCollation),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithoutCollation)
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntitiesWithCollation.OrderBy(e => e.ColumnWithCollation).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.ColumnWithoutCollation, e.ColumnWithCollation)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new", "ABC"),
                                                                                                          (_TEST_ENTITY_ID, "old 1", "abc"));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_entities_with_composite_primary_key(bool useTransactionScope)
   {
      ArrangeDbContext.CompositeEntities.AddRange(new MySqlCompositeEntity { RegionId = 1, ItemId = 1, Name = "1-1" },
                                                  new MySqlCompositeEntity { RegionId = 1, ItemId = 2, Name = "1-2" },
                                                  new MySqlCompositeEntity { RegionId = 2, ItemId = 1, Name = "2-1" },
                                                  new MySqlCompositeEntity { RegionId = 2, ItemId = 2, Name = "2-2" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await UpdateAsync([
                                              new MySqlCompositeEntity { RegionId = 1, ItemId = 2, Name = "changed 1-2" },
                                              new MySqlCompositeEntity { RegionId = 2, ItemId = 1, Name = "changed 2-1" },
                                              new MySqlCompositeEntity { RegionId = 3, ItemId = 1, Name = "missing 3-1" }
                                           ],
                                           new MySqlBulkUpdateOptions(),
                                           useTransactionScope);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.CompositeEntities.OrderBy(e => e.RegionId).ThenBy(e => e.ItemId).ToListAsync();
      loadedEntities.Select(e => (e.RegionId, e.ItemId, e.Name)).Should().Equal((1, 1, "1-1"),
                                                                                (1, 2, "changed 1-2"),
                                                                                (2, 1, "changed 2-1"),
                                                                                (2, 2, "2-2"));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_match_null_keys_null_safe(bool useTransactionScope)
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = null, Count = 0 };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 0 };
      var entity_3 = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 6, Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2, entity_3);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await UpdateAsync([
                                              new TestEntity { NullableCount = null, Count = 10 },
                                              new TestEntity { NullableCount = 5, Count = 20 },
                                              new TestEntity { NullableCount = 7, Count = 30 }
                                           ],
                                           new MySqlBulkUpdateOptions
                                           {
                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.NullableCount),
                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                           },
                                           useTransactionScope);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ThenBy(e => e.NullableCount).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.NullableCount, e.Count)).Should().Equal((_TEST_ENTITY_ID, (int?)null, 10),
                                                                                  (_OTHER_TEST_ENTITY_ID, 5, 20),
                                                                                  (_THIRD_TEST_ENTITY_ID, 6, 0));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_match_keys_with_converter(bool useTransactionScope)
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1), Count = 0 };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(2), Count = 0 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await UpdateAsync([
                                              new TestEntity { ConvertibleClass = new ConvertibleClass(2), Count = 20 },
                                              new TestEntity { ConvertibleClass = new ConvertibleClass(3), Count = 30 }
                                           ],
                                           new MySqlBulkUpdateOptions
                                           {
                                              KeyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.ConvertibleClass),
                                              PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count)
                                           },
                                           useTransactionScope);

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Count)).Should().Equal((_TEST_ENTITY_ID, 0), (_OTHER_TEST_ENTITY_ID, 20));
   }

   [Fact]
   public async Task Should_update_entity_with_auto_increment()
   {
      var entity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name";

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      // The staging table of a regular entity must not renumber explicit ids.
      ExecutedCommands.Should().ContainSingle(command => command.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal))
                      .Which.Should().NotContain("AUTO_INCREMENT");

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Id.Should().NotBe(0);
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithAutoIncrement
                                           {
                                              Id = loadedEntity.Id,
                                              Name = "Name"
                                           });
   }

   [Fact]
   public async Task Should_update_entity_with_auto_increment_having_custom_property_selector()
   {
      var entity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name";

      var affectedRows = await SUT.BulkUpdateAsync([entity],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Id),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => new
                                                                                                                                               {
                                                                                                                                                  e.Id,
                                                                                                                                                  e.Name
                                                                                                                                               })
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Id.Should().NotBe(0);
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithAutoIncrement
                                           {
                                              Id = entity.Id,
                                              Name = "Name"
                                           });
   }

   [Fact]
   public async Task Should_update_private_property()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.SetPrivateField(1);

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.GetPrivateField().Should().Be(1);
   }

   [Fact]
   public async Task Should_update_shadow_properties()
   {
      var entity = new TestEntityWithShadowProperties { Id = _TEST_ENTITY_ID };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      ActDbContext.Entry(entity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(entity).Property("ShadowIntProperty").CurrentValue = 42;

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.SingleAsync();
      AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
   }

   [Fact]
   public async Task Should_update_column_with_converter()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.ConvertibleClass = new ConvertibleClass(43);

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity> { entity }, new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.ConvertibleClass.Should().NotBeNull();
      loadedEntity.ConvertibleClass!.Key.Should().Be(43);
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

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().BeNull();
      loadedEntity.NullableCount.Should().BeNull();
      loadedEntity.ConvertibleClass.Should().BeNull();
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_all_kinds_of_properties(bool useTransactionScope)
   {
      var entity_1 = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name", RequiredName = "RequiredName", Count = 1, NullableCount = 1, ConvertibleClass = new ConvertibleClass(1) };
      var entity_2 = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName" };
      var untouched = new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "Untouched", RequiredName = "RequiredName", Count = 3 };
      ArrangeDbContext.AddRange(entity_1, entity_2, untouched);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = null;
      entity_1.RequiredName = "Changed";
      entity_1.Count = 42;
      entity_1.NullableCount = null;
      entity_1.ConvertibleClass = new ConvertibleClass(43);
      entity_1.PropertyWithBackingField = 7;
      entity_1.SetPrivateField(3);

      entity_2.Name = "O'Brien \\ \"quoted\" `tick`";
      entity_2.NullableCount = 5;
      entity_2.ConvertibleClass = null;
      entity_2.SetPrivateField(9);

      var missing = new TestEntity { Id = _MISSING_TEST_ENTITY_ID, RequiredName = "Missing", Count = 100 };

      var affectedRows = await UpdateAsync([entity_1, missing, entity_2], new MySqlBulkUpdateOptions(), useTransactionScope);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().HaveCount(3);
      loadedEntities[0].Should().BeEquivalentTo(entity_2);
      loadedEntities[0].GetPrivateField().Should().Be(9);
      loadedEntities[1].Should().BeEquivalentTo(untouched);
      loadedEntities[1].GetPrivateField().Should().Be(0);
      loadedEntities[2].Should().BeEquivalentTo(entity_1);
      loadedEntities[2].GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_update_entity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"), new BoundaryValueObject(2, 5));

      ArrangeDbContext.Add(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      testEntity.Boundary = new BoundaryValueObject(10, 20);

      var affectedRows = await SUT.BulkUpdateAsync([testEntity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_update_property_of_base_class()
   {
      var entity = new TestEntityWithBaseClass { Id = new Guid("3D3AECE7-3B5A-48C6-9C8C-CAB7FFE2120D"), Name = "Initial" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "changed";

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions
                                                             {
                                                                PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithBaseClass>(e => e.Name)
                                                             });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithBaseClass.SingleAsync();
      loadedEntity.Name.Should().Be("changed");
   }

   [Fact]
   public async Task Should_update_flags_enum()
   {
      var entity = new TestEntityWithFlags { Id = _TEST_ENTITY_ID, GroupId = 1, Phase = PhaseMembership.A };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.GroupId = 2;
      entity.Phase = PhaseMembership.A | PhaseMembership.C;

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.SingleAsync();
      loadedEntity.GroupId.Should().Be(2);
      loadedEntity.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_entities_with_different_column_names(bool useTransactionScope)
   {
      var entity_1 = new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Name 1", Count = 1 };
      var entity_2 = new TestEntityWithDifferentColumnNames { Id = _OTHER_TEST_ENTITY_ID, Name = "Name 2", Count = 2 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = null;
      entity_1.Count = 10;
      entity_2.Name = "Changed 2";

      var affectedRows = await UpdateAsync([entity_1, entity_2], new MySqlBulkUpdateOptions(), useTransactionScope);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity_2, entity_1], config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_match_on_non_pk_property_with_different_column_name()
   {
      ArrangeDbContext.TestEntitiesWithDifferentColumnNames.AddRange(new TestEntityWithDifferentColumnNames { Id = _TEST_ENTITY_ID, Name = "Name 1", Count = 1 },
                                                                     new TestEntityWithDifferentColumnNames { Id = _OTHER_TEST_ENTITY_ID, Name = "Name 2", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkUpdateAsync([new TestEntityWithDifferentColumnNames { Id = _MISSING_TEST_ENTITY_ID, Name = "Changed", Count = 2 }],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithDifferentColumnNames>(e => e.Count),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithDifferentColumnNames>(e => e.Name)
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Name, e.Count)).Should().Equal((_TEST_ENTITY_ID, "Name 1", 1),
                                                                         (_OTHER_TEST_ENTITY_ID, "Changed", 2));
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

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

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
   public async Task Should_update_entity_with_json_column()
   {
      var entity = new TestEntityWithJsonColumns
                   {
                      Id = _TEST_ENTITY_ID,
                      JsonColumn = """{"key": "original"}"""
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.JsonColumn = """{"key": "updated"}""";

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      // MySQL normalizes the JSON text; the value is already in the normalized form.
      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.JsonColumn.Should().Be("""{"key": "updated"}""");
   }

   [Fact]
   public async Task Should_accept_options_of_other_providers()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Name = "Name"; // this would not be updated
      entity.Count = 42;

      var affectedRows = await SUT.BulkUpdateAsync([entity],
                                                   new GenericBulkOperationOptions
                                                   {
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntity { Id = _TEST_ENTITY_ID, Name = "original value", RequiredName = "RequiredName", Count = 42 });
   }

   [Fact]
   public async Task Should_update_entities_in_table_name_override()
   {
      await CreateRedirectTableAsync("TestEntities_BulkUpdateRedirect", "OldName");

      try
      {
         var entity = new TestEntity
                      {
                         Id = _TEST_ENTITY_ID,
                         RequiredName = "RequiredName",
                         Name = "UpdatedName",
                         Count = 99
                      };

         var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { TableName = "TestEntities_BulkUpdateRedirect" });

         affectedRows.Should().Be(1);

         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         (await LoadColumnAsync<string>("TestEntities_BulkUpdateRedirect", "Name")).Should().Equal("UpdatedName");
         (await LoadColumnAsync<int>("TestEntities_BulkUpdateRedirect", "Count")).Should().Equal(99);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpdateRedirect");
      }
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_entities_in_table_from_table_name_provider(bool useGenericOptions)
   {
      await CreateRedirectTableAsync("TestEntities_BulkUpdateRedirect", "OldName");

      try
      {
         var entity = new TestEntity
                      {
                         Id = _TEST_ENTITY_ID,
                         RequiredName = "RequiredName",
                         Name = "UpdatedName",
                         Count = 99
                      };
         var provider = new SuffixTableNameProvider("_BulkUpdateRedirect");

         IBulkUpdateOptions options = useGenericOptions
                                         ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                         : new MySqlBulkUpdateOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkUpdateAsync([entity], options);

         affectedRows.Should().Be(1);

         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         (await LoadColumnAsync<string>("TestEntities_BulkUpdateRedirect", "Name")).Should().Equal("UpdatedName");
         provider.RequestedEntityTypes.Should().ContainSingle();
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpdateRedirect");
      }
   }

   [Fact]
   public async Task Should_update_entities_in_table_of_the_database_from_ForRootEntity_provider()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database.
      var database = await ActDbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() AS `Value`").SingleAsync();
      await CreateRedirectTableAsync("TestEntities_BulkUpdateRedirect", "OldName");

      try
      {
         var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "UpdatedName" };

         var affectedRows = await SUT.BulkUpdateAsync([entity],
                                                      new MySqlBulkUpdateOptions
                                                      {
                                                         TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkUpdateRedirect", database)
                                                      });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>("TestEntities_BulkUpdateRedirect", "Name")).Should().Equal("UpdatedName");
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkUpdateRedirect");
      }
   }

   [Fact]
   public async Task Should_throw_when_trying_to_update_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      // The entity is keyless, but the missing table name is the problem to report (as the other providers do).
      await SUT.Awaiting(sut => sut.BulkUpdateAsync(new List<TempTable<int>> { new(0) }, new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_throw_when_trying_to_update_keyed_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      await SUT.Awaiting(sut => sut.BulkUpdateAsync(new[] { new TestEntityTempTable { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" } }, new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_update_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntityTempTable
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "UpdatedName",
                          RequiredName = "RequiredName",
                          Count = 99
                       };

      var affectedRows = await SUT.BulkUpdateAsync(new[] { tempEntity }, new MySqlBulkUpdateOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[0].Name.Should().Be("UpdatedName");
      loadedEntities[0].Count.Should().Be(99);
   }

   [Fact]
   public async Task Should_update_temp_table_entity_with_ForRootEntity_provider()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntityTempTable
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "UpdatedName",
                          RequiredName = "RequiredName",
                          Count = 99
                       };

      var affectedRows = await SUT.BulkUpdateAsync(new[] { tempEntity }, new MySqlBulkUpdateOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities") });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().Be("UpdatedName");
      loadedEntity.Count.Should().Be(99);
   }

   [Fact]
   public async Task Should_throw_on_update_of_inlined_owned_type()
   {
      ArrangeDbContext.TestEntities_Own_Inline.Add(new TestEntity_Owns_Inline { Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), InlineEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_Inline.SingleAsync();
      entity.InlineEntity.IntColumn = 2;

      // The staging temp table doesn't support owned types (same as with PostgreSQL and SQL Server).
      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("Temp tables don't support owned entities.");

      (await AssertDbContext.TestEntities_Own_Inline.SingleAsync()).InlineEntity.IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_update_of_owned_type_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateOne.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"), SeparateEntity = new OwnedEntity { IntColumn = 1, StringColumn = "old" } });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateOne.SingleAsync();
      entity.SeparateEntity!.IntColumn = 2;

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("*owned entities mapped to separate tables*");

      (await AssertDbContext.TestEntities_Own_SeparateOne.SingleAsync()).SeparateEntity!.IntColumn.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_on_update_of_owned_collection_in_separate_table()
   {
      ArrangeDbContext.TestEntities_Own_SeparateMany.Add(new TestEntity_Owns_SeparateMany { Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"), SeparateEntities = [new() { IntColumn = 1, StringColumn = "old" }] });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.TestEntities_Own_SeparateMany.SingleAsync();
      entity.SeparateEntities[0].IntColumn = 2;

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("*owned entities mapped to separate tables*");

      (await AssertDbContext.TestEntities_Own_SeparateMany.SingleAsync()).SeparateEntities.Single().IntColumn.Should().Be(1);
   }

   // The tests below cover behavior specific to the MySQL executor: staging table, batching, transactions, command timeout.

   [Theory]
   [InlineData(1, null, 1)]
   [InlineData(500, null, 1)] // the default batch size
   [InlineData(501, null, 2)]
   [InlineData(10, 1, 10)]
   [InlineData(10, 3, 4)]
   [InlineData(10, 100, 1)]
   public async Task Should_stage_rows_in_batches_according_to_batch_size(int rowCount, int? batchSize, int expectedStatements)
   {
      var entities = Enumerable.Range(0, rowCount).Select(count => new TestEntity { Id = Guid.NewGuid(), RequiredName = "RequiredName", Count = count }).ToList();
      ArrangeDbContext.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();

      foreach (var entity in entities)
      {
         entity.Count += 1000;
      }

      // The status counters are per session. The connection stays open, so the executor and the status queries share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkUpdateAsync(entities, new MySqlBulkUpdateOptions { BatchSize = batchSize, PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count) });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(rowCount);
      statements.Should().Be(expectedStatements);

      var loadedCounts = await AssertDbContext.TestEntities.Select(e => e.Count).OrderBy(c => c).ToListAsync();
      loadedCounts.Should().Equal(Enumerable.Range(1000, rowCount));
   }

   [Fact]
   public async Task Should_limit_rows_per_staging_statement_to_the_parameter_limit_of_mysql()
   {
      // The staging table has all columns of the entity and MySQL accepts 65,535 parameters per statement, so the number of rows per statement is limited by the number of columns.
      var columnCount = ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetProperties().Count();
      var maximumRows = 65_535 / columnCount;
      var entities = Enumerable.Range(0, maximumRows + 1).Select(count => new TestEntity { Id = Guid.NewGuid(), RequiredName = "RequiredName", Count = count }).ToList();
      await ArrangeDbContext.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(entities, new MySqlBulkInsertOptions());

      foreach (var entity in entities)
      {
         entity.Count += 100_000;
      }

      await ActDbContext.Database.OpenConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkUpdateAsync(entities, new MySqlBulkUpdateOptions { BatchSize = Int32.MaxValue });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(maximumRows + 1);
      statements.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync(e => e.Count >= 100_000)).Should().Be(maximumRows + 1);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   public async Task Should_throw_if_batch_size_is_not_positive(int batchSize)
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { BatchSize = batchSize }))
               .Should().ThrowAsync<ArgumentOutOfRangeException>()
               .WithMessage("*batch size*greater than 0*");

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_drop_the_staging_table_after_the_update()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      // A temp table belongs to the session. The connection stays open, so the executor and the query below share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());
      var stagingTable = ExecutedCommands.Should().ContainSingle(command => command.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal)).Subject;
      var tableName = stagingTable.Split('`')[1];

      // A temporary table exists for the session only; if the executor didn't drop it, the table could still be queried.
      var act = () => ActDbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM `{tableName}`");
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1146);
   }

   [Fact]
   public async Task Should_update_entities_with_command_timeout()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(30) });

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_apply_command_timeout_to_update_statement()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      // The uncommitted update of another transaction locks the row until the lock wait timeout of the server (50 seconds by default) is reached.
      await using var blockingTransaction = await ArrangeDbContext.Database.BeginTransactionAsync();
      await ArrangeDbContext.Database.ExecuteSqlRawAsync("UPDATE `TestEntities` SET `Name` = 'Blocking' WHERE `Id` = {0}", _TEST_ENTITY_ID);

      var stopwatch = Stopwatch.StartNew();

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(1) }))
               .Should().ThrowAsync<MySqlException>();

      stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
   }

   [Fact]
   public async Task Should_throw_when_cancellation_is_requested()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions(), cancellationTokenSource.Token))
               .Should().ThrowAsync<OperationCanceledException>();

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_not_update_any_row_if_a_later_batch_of_the_staging_table_fails()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "A" }, new MySqlTestEntity { Id = 2, Name = "B" }, new MySqlTestEntity { Id = 3, Name = "C" });
      await ArrangeDbContext.SaveChangesAsync();

      var act = () => SUT.BulkUpdateAsync([
                                             new MySqlTestEntity { Id = 1, Name = "A changed" },
                                             new MySqlTestEntity { Id = 2, Name = "B changed" },
                                             new MySqlTestEntity { Id = 3, Name = new string('x', 300) }
                                          ],
                                          new MySqlBulkUpdateOptions { BatchSize = 1 });

      // 1406: data too long for the column of the staging table (strict mode)
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1406);

      var names = await AssertDbContext.Entities.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync();
      names.Should().Equal("A", "B", "C");
   }

   [Fact]
   public async Task Should_not_apply_any_update_if_the_update_statement_fails()
   {
      ArrangeDbContext.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "A" }, new MySqlTestEntity { Id = 2, Name = "B" }, new MySqlTestEntity { Id = 3, Name = "C" });
      await ArrangeDbContext.SaveChangesAsync();

      // The name of MySqlTestEntity has a unique index. Row 1 can be updated, row 2 collides with row 3.
      var act = () => SUT.BulkUpdateAsync([
                                             new MySqlTestEntity { Id = 1, Name = "A changed" },
                                             new MySqlTestEntity { Id = 2, Name = "C" }
                                          ],
                                          new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTestEntity>(e => e.Name) });

      // 1062: duplicate entry for the unique index
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      var names = await AssertDbContext.Entities.OrderBy(e => e.Id).Select(e => e.Name).ToListAsync();
      names.Should().Equal("A", "B", "C");
   }

   [Fact]
   public async Task Should_not_apply_any_update_if_a_foreign_key_is_violated()
   {
      var parent = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Parent", Count = 1 };
      var child = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Child", Count = 2, ParentId = _TEST_ENTITY_ID };
      ArrangeDbContext.AddRange(parent, child);
      await ArrangeDbContext.SaveChangesAsync();

      parent.Count = 10;
      child.Count = 20;
      child.ParentId = _MISSING_TEST_ENTITY_ID;

      var act = () => SUT.BulkUpdateAsync([parent, child], new MySqlBulkUpdateOptions());

      // 1452: cannot add or update a child row, a foreign key constraint fails
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1452);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Count, e.ParentId)).Should().Equal((_TEST_ENTITY_ID, 1, (Guid?)null),
                                                                             (_OTHER_TEST_ENTITY_ID, 2, _TEST_ENTITY_ID));
   }

   [Fact]
   public async Task Should_throw_if_the_input_contains_the_same_primary_key_twice()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // The staging table has the primary key of the entity.
      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                          ],
                                          new MySqlBulkUpdateOptions());

      // 1062: duplicate entry for the primary key of the staging table
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_and_commit_with_the_transaction_of_the_caller()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.CommitAsync();

      AssertDbContext.ChangeTracker.Clear();
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_not_update_rows_if_the_transaction_of_the_caller_is_rolled_back()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

      affectedRows.Should().Be(1);

      await transaction.RollbackAsync();

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_rows_inside_a_transaction_scope_without_a_staging_table()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      var affectedRows = await UpdateAsync([entity], new MySqlBulkUpdateOptions(), true);

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(42);
      ExecutedCommands.Should().NotContain(command => command.Contains("TEMPORARY", StringComparison.Ordinal), "MySQL rejects temp tables inside XA transactions");
   }

   [Fact]
   public async Task Should_roll_back_the_update_if_the_transaction_scope_is_not_completed()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

         affectedRows.Should().Be(1);
      }

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_apply_duplicates_of_a_key_in_order_inside_a_transaction_scope()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // Row by row there is no staging table with a primary key, so every row is applied, the last one wins.
      var affectedRows = await UpdateAsync([
                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                              new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                           ],
                                           new MySqlBulkUpdateOptions(),
                                           true);

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(2);
   }

   private async Task<int> UpdateAsync<T>(IReadOnlyList<T> entities, IBulkUpdateOptions options, bool useTransactionScope)
      where T : class
   {
      if (!useTransactionScope)
         return await SUT.BulkUpdateAsync(entities, options);

      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var affectedRows = await SUT.BulkUpdateAsync(entities, options);
      scope.Complete();

      return affectedRows;
   }

   // The status counters are per session; see Should_stage_rows_in_batches_according_to_batch_size.
   private async Task<long> GetSessionStatusAsync(string name)
   {
      await using var command = ActDbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = $"SHOW SESSION STATUS LIKE '{name}'";
      await using var reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();

      return Int64.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
   }

   // Creates a copy of the table "TestEntities" with the rows of that table (the entity is the only one of the test) and empties the original table,
   // so that an update is visible in the copy only.
   private async Task CreateRedirectTableAsync(string tableName, string name)
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = name });
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
