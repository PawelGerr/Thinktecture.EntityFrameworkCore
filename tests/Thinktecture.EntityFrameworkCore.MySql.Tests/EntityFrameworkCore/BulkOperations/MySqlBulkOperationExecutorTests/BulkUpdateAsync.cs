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

// The executor stages the rows in a temporary table ("CREATE TEMPORARY TABLE ... SELECT ... FROM target LIMIT 0") and runs one
// "UPDATE target INNER JOIN staging" statement. EF Core executes these statements, so they are visible in ExecutedCommands. Only the
// INSERT statements that fill the staging table use raw ADO commands. The tests verify the resulting rows and the return values (the number of matched rows, i.e. rows with unchanged values are counted as well).
// MySQL rejects temporary tables inside XA transactions, so the tests with a TransactionScope use a connection with "UseXaTransactions=false".
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkUpdateAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("5B9587A3-2312-43DF-9681-38EC22AD8606");
   private static readonly Guid _MISSING_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkUpdateAsync(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
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
      ArrangeDbContext.TypedEntities.Add(new MySqlTypedEntity { Id = _TEST_ENTITY_ID, Amount = 1, Flag = false });
      await ArrangeDbContext.SaveChangesAsync();

      // The amount does not fit into the column decimal(18,4). If it were staged, the staging insert would fail with error 1264 (out of range).
      var affectedRows = await SUT.BulkUpdateAsync([new MySqlTypedEntity { Id = _TEST_ENTITY_ID, Amount = 100_000_000_000_000_000_000m, Flag = true }],
                                                   new MySqlBulkUpdateOptions { PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTypedEntity>(e => e.Flag) });

      affectedRows.Should().Be(1);

      var createStatement = ExecutedCommands.Should().ContainSingle(command => command.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal)).Subject;
      createStatement.Should().Contain("SELECT `Id`, `Flag` FROM `TypedEntities` LIMIT 0");

      var loadedEntity = await AssertDbContext.TypedEntities.SingleAsync();
      loadedEntity.Flag.Should().BeTrue();
      loadedEntity.Amount.Should().Be(1);
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

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_nullable_ComplexTypes_to_null_and_to_values(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var toNulls = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0001"));
      var toValues = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("6E3A0C4B-1F6F-4A7D-8E46-8C3B7D2F0002"));
      ArrangeDbContext.AddRange(toNulls, toValues);
      await ArrangeDbContext.SaveChangesAsync();

      var updated = new[] { TestEntityWithNullableComplexTypes.CreateWithNulls(toNulls.Id), TestEntityWithNullableComplexTypes.CreateWithValues(toValues.Id) };

      var affectedRows = await SUT.BulkUpdateAsync(updated, options);

      affectedRows.Should().Be(2);
      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync();
      loaded.Should().BeEquivalentTo(updated);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_struct_members_of_null_ComplexType_to_null(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var toNulls = TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0021"));
      var toValues = TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0022"));
      ArrangeDbContext.AddRange(toNulls, toValues);
      await ArrangeDbContext.SaveChangesAsync();

      var updated = new[] { TestEntityWithStructsInOptionalComplexType.CreateWithNulls(toNulls.Id), TestEntityWithStructsInOptionalComplexType.CreateWithValues(toValues.Id) };

      var affectedRows = await SUT.BulkUpdateAsync(updated, options);

      affectedRows.Should().Be(2);
      var loaded = await AssertDbContext.TestEntitiesWithStructsInOptionalComplexType.ToListAsync();
      loaded.Should().BeEquivalentTo(updated);

      var structColumns = await MySqlStructColumns.Query(AssertDbContext, "TestEntitiesWithStructsInOptionalComplexType").ToListAsync();
      structColumns.Should().BeEquivalentTo(updated.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
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
   public async Task Should_update_entities_in_table_of_the_database_from_ForRootEntity_provider_if_SchemaBehavior_is_Ignore()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database (here: the current one).
      // SchemaBehavior Ignore drops the schema; the default (Throw) rejects it.
      ConfigureMySql = builder => builder.SchemaBehavior(MySqlSchemaBehavior.Ignore);
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

   // The staging table has the key and the updated column, so the default budget of 5,000 parameters gives 2,500 rows per statement.
   [Theory]
   [InlineData(1, null, null, 1)]
   [InlineData(2500, null, null, 1)]
   [InlineData(2501, null, null, 2)]
   [InlineData(10, null, 2, 10)]
   [InlineData(10, null, 6, 4)]
   [InlineData(10, null, 1, 10)] // a budget below the column count gives 1 row per statement
   [InlineData(10, null, 200, 1)]
   [InlineData(10, 6, null, 4)] // the registered budget
   [InlineData(10, 2, 200, 1)] // the budget of the options overrides the registered budget
   public async Task Should_stage_rows_in_statements_according_to_the_parameter_budget(int rowCount, int? registeredBudget, int? optionBudget, int expectedStatements)
   {
      if (registeredBudget is not null)
         ConfigureMySql = builder => builder.AddBulkOperationSupport(maxParametersPerStatement: registeredBudget.Value);

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
      var affectedRows = await SUT.BulkUpdateAsync(entities, new MySqlBulkUpdateOptions { MaxParametersPerStatement = optionBudget, PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count) });
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
      var affectedRows = await SUT.BulkUpdateAsync(entities, new MySqlBulkUpdateOptions { MaxParametersPerStatement = 65_535 });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(maximumRows + 1);
      statements.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync(e => e.Count >= 100_000)).Should().Be(maximumRows + 1);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(65_536)]
   public void Should_throw_if_the_parameter_budget_is_out_of_range(int maxParametersPerStatement)
   {
      var act = () => new MySqlBulkUpdateOptions { MaxParametersPerStatement = maxParametersPerStatement };

      act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
   }

   [Fact]
   public async Task Should_drop_the_staging_table_after_the_update()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      // A temp table belongs to the session. The connection stays open, so both updates and the query below share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());
      var stagingTable = ExecutedCommands.Should().ContainSingle(command => command.StartsWith("CREATE TEMPORARY TABLE", StringComparison.Ordinal)).Subject.Split('`')[1];
      ExecutedCommands.Should().Contain($"DROP TEMPORARY TABLE IF EXISTS `{stagingTable}`;");

      // The second update leases the same name again. If the first staging table still existed, its CREATE would fail with error 1050.
      entity.Count = 43;
      (await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions())).Should().Be(1);

      // A temporary table exists for the session only; if the executor didn't drop it, the table could still be queried.
      var act = () => ActDbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM `{stagingTable}`");
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1146);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(43);
   }

   [Fact]
   public async Task Should_drop_the_staging_table_if_the_update_fails()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // A temp table belongs to the session. The connection stays open, so both updates share the session.
      await ActDbContext.Database.OpenConnectionAsync();

      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                          ],
                                          new MySqlBulkUpdateOptions());
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      // The second update leases the same name again. If the staging table of the failed update still existed, its CREATE would fail with error 1050.
      entity.Count = 42;
      (await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions())).Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(42);
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
   public async Task Should_apply_command_timeout_of_the_context_to_bulk_update()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;

      // The uncommitted update of another transaction locks the row until the lock wait timeout of the server (50 seconds by default) is reached.
      // The default timeout of a MySqlCommand is 30 seconds.
      await using var blockingTransaction = await ArrangeDbContext.Database.BeginTransactionAsync();
      await ArrangeDbContext.Database.ExecuteSqlRawAsync("UPDATE `TestEntities` SET `Name` = 'Blocking' WHERE `Id` = {0}", _TEST_ENTITY_ID);

      // The provider copies the timeout of the context into the connection string when it creates the connection, so the timeout is set afterwards.
      ActDbContext.Database.GetDbConnection();
      ActDbContext.Database.SetCommandTimeout(1);

      var stopwatch = Stopwatch.StartNew();

      await SUT.Awaiting(sut => sut.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions()))
               .Should().ThrowAsync<MySqlException>();

      stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
   }

   [Fact]
   public async Task Should_restore_the_command_timeout_of_the_context_after_the_update()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;
      ActDbContext.Database.SetCommandTimeout(17);

      await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(30) });

      ActDbContext.Database.GetCommandTimeout().Should().Be(17);
   }

   [Fact]
   public async Task Should_restore_the_command_timeout_of_the_context_after_a_failed_update()
   {
      ArrangeDbContext.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      ActDbContext.Database.SetCommandTimeout(17);

      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                          ],
                                          new MySqlBulkUpdateOptions { CommandTimeout = TimeSpan.FromSeconds(30) });
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      ActDbContext.Database.GetCommandTimeout().Should().Be(17);
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
                                          new MySqlBulkUpdateOptions { MaxParametersPerStatement = 1 });

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

      // The staging table has a unique index on the key columns.
      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 1 },
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Count = 2 }
                                          ],
                                          new MySqlBulkUpdateOptions());

      // 1062: duplicate entry for the unique index of the staging table
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
   public async Task Should_roll_back_the_update_if_the_transaction_scope_is_not_completed()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.Count = 42;
      UseConnectionWithoutXaTransactions();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await SUT.BulkUpdateAsync([entity], new MySqlBulkUpdateOptions());

         affectedRows.Should().Be(1);
      }

      (await AssertDbContext.TestEntities.SingleAsync()).Count.Should().Be(0);
   }

   [Fact]
   public async Task Should_stage_rows_with_the_column_definitions_of_the_target_table()
   {
      ArrangeDbContext.TestEntitiesWithCollation.AddRange(new TestEntityWithCollation(_TEST_ENTITY_ID, "old 1", "abc"),
                                                          new TestEntityWithCollation(_OTHER_TEST_ENTITY_ID, "old 2", "ABC"));
      await ArrangeDbContext.SaveChangesAsync();

      // The key column is case-sensitive (utf8mb4_bin) and the database default is not. The staging table copies the collation of the column,
      // so its unique key index accepts both keys and each input row matches its own row only.
      var affectedRows = await SUT.BulkUpdateAsync([
                                                      new TestEntityWithCollation(_MISSING_TEST_ENTITY_ID, "new 2", "ABC"),
                                                      new TestEntityWithCollation(_THIRD_TEST_ENTITY_ID, "new 1", "abc")
                                                   ],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithCollation),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithCollation>(e => e.ColumnWithoutCollation)
                                                   });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithCollation.OrderBy(e => e.ColumnWithCollation).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.ColumnWithoutCollation, e.ColumnWithCollation)).Should().Equal((_OTHER_TEST_ENTITY_ID, "new 2", "ABC"),
                                                                                                          (_TEST_ENTITY_ID, "new 1", "abc"));
   }

   [Fact]
   public async Task Should_throw_if_entities_contain_duplicate_non_null_values_of_nullable_key()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // The staging table has a unique index on the nullable key column.
      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 1 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "RequiredName", NullableCount = 5, Count = 2 }
                                          ],
                                          new MySqlBulkUpdateOptions
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
      var affectedRows = await SUT.BulkUpdateAsync([
                                                      new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 10, String = "a", NullableInt = null, NullableString = "x" },
                                                      new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 20, String = "b", NullableInt = 1, NullableString = null },
                                                      new TestEntityWithSqlDefaultValues { Id = _MISSING_TEST_ENTITY_ID, Int = 30, String = "c", NullableInt = 1, NullableString = "x" }
                                                   ],
                                                   new MySqlBulkUpdateOptions
                                                   {
                                                      KeyProperties = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new { e.NullableInt, e.NullableString }),
                                                      PropertiesToUpdate = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Int)
                                                   });

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDefaultValues.OrderBy(e => e.String).ToListAsync();
      loadedEntities.Select(e => (e.Id, e.Int)).Should().Equal((_TEST_ENTITY_ID, 10), (_OTHER_TEST_ENTITY_ID, 20), (_THIRD_TEST_ENTITY_ID, 30));
   }

   [Fact]
   public async Task Should_throw_if_entities_contain_duplicate_auto_increment_keys()
   {
      var entity = new TestEntityWithAutoIncrement { Name = "original value" };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // Bulk update keeps the unique key index also for an AUTO_INCREMENT key.
      var act = () => SUT.BulkUpdateAsync([
                                             new TestEntityWithAutoIncrement { Id = entity.Id, Name = "first" },
                                             new TestEntityWithAutoIncrement { Id = entity.Id, Name = "second" }
                                          ],
                                          new MySqlBulkUpdateOptions());

      // 1062: duplicate entry for the unique index of the staging table
      (await act.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntitiesWithAutoIncrement.SingleAsync()).Name.Should().Be("original value");
   }

   [Fact]
   public async Task Should_throw_if_key_is_wider_than_the_index_limit()
   {
      const string tableName = "TestEntities_with_wide_key";

      // varchar(1000) gets the unique key index of the staging table. With utf8mb4 the key has 4,000 bytes, more than the limit of 3,072 bytes.
      ConfigureModel = builder => builder.Entity<TestEntity>().Property(e => e.Name).HasMaxLength(1000);
      ArrangeDbContext.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "key", RequiredName = "RequiredName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      // A run that was killed before its cleanup leaves the table behind; CREATE TABLE would then fail with error 1050.
      await DropTablesAsync(tableName);
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(tableName)} LIKE `TestEntities`");

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE {Quote(tableName)} MODIFY `Name` varchar(1000) CHARACTER SET utf8mb4 NULL");
         await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(tableName)} SELECT * FROM `TestEntities`");

         var entities = new[] { new TestEntity { Id = _TEST_ENTITY_ID, Name = "key", RequiredName = "RequiredName", Count = 2 } };
         var keyProperties = IEntityPropertiesProvider.Include<TestEntity>(e => e.Name);
         var propertiesToUpdate = IEntityPropertiesProvider.Include<TestEntity>(e => e.Count);
         var tableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tableName);

         var update = () => SUT.BulkUpdateAsync(entities, new MySqlBulkUpdateOptions { KeyProperties = keyProperties, PropertiesToUpdate = propertiesToUpdate, TableNameProvider = tableNameProvider });
         var upsert = () => SUT.BulkInsertOrUpdateAsync(entities, new MySqlBulkInsertOrUpdateOptions { KeyProperties = keyProperties, PropertiesToUpdate = propertiesToUpdate, TableNameProvider = tableNameProvider });

         // 1071: specified key was too long
         (await update.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1071);
         (await upsert.Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(1071);

         (await LoadColumnAsync<int>(tableName, "Count")).Should().Equal(1);
      }
      finally
      {
         await DropTablesAsync(tableName);
      }
   }

   private async Task<int> UpdateAsync<T>(IReadOnlyList<T> entities, IBulkUpdateOptions options, bool useTransactionScope)
      where T : class
   {
      if (!useTransactionScope)
         return await SUT.BulkUpdateAsync(entities, options);

      UseConnectionWithoutXaTransactions();
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var affectedRows = await SUT.BulkUpdateAsync(entities, options);
      scope.Complete();

      return affectedRows;
   }

   // MySQL rejects temporary tables inside XA transactions. Without XA, MySqlConnector enlists the connection with a plain "START TRANSACTION".
   private void UseConnectionWithoutXaTransactions()
   {
      var builder = new MySqlConnectionStringBuilder(ConnectionString) { UseXaTransactions = false };
      ActDbContext.Database.SetConnectionString(builder.ConnectionString);
   }

   // The status counters are per session; see Should_stage_rows_in_statements_according_to_the_parameter_budget.
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

      // A run that was killed before its cleanup leaves the table behind; CREATE TABLE would then fail with error 1050.
      await DropTablesAsync(tableName);
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(tableName)} LIKE `TestEntities`");
      await ActDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {Quote(tableName)} SELECT * FROM `TestEntities`");
      await ActDbContext.Database.ExecuteSqlRawAsync("DELETE FROM `TestEntities`");
   }

   // Selects the insert path of the staging fill (multi-row INSERT or MySqlBulkCopy); call it before the first context access.
   private MySqlBulkUpdateOptions CreateOptions(bool useBulkCopy)
   {
      UseBulkCopy = useBulkCopy;
      return new MySqlBulkUpdateOptions();
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
