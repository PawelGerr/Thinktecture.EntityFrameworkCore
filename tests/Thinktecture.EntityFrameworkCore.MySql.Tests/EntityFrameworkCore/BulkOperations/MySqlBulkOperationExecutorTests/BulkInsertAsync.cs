using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// The executor writes with raw ADO commands (multi-row INSERT or MySqlBulkCopy), so these statements are not visible in ExecutedCommands.
// The tests verify the resulting rows, the return values and, where the number of statements matters, the status counters of the MySQL session.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertAsync : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _INLINE_OWNER_ID = new("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3");
   private static readonly Guid _SEPARATE_OWNER_ID = new("7C00ABFE-875B-4396-BE51-3E898647A264");
   private static readonly Guid _NESTED_OWNER_ID = new("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7");

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkInsertAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new List<TempTable<int>> { new(0) }, new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_throw_when_type_is_not_in_model()
   {
      await SUT.Awaiting(sut => sut.BulkInsertAsync(new List<TestEntityTempTable> { new() }, new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<EntityTypeNotFoundException>();
   }

   [Fact]
   public async Task Should_insert_entities()
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };

      var testEntities = new[] { testEntity };

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions());

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
   public async Task Should_insert_column_with_converter()
   {
      var entities = new List<TestEntity> { new() { ConvertibleClass = new ConvertibleClass(42), RequiredName = "RequiredName" } };

      await SUT.BulkInsertAsync(entities, new MySqlBulkInsertOptions());

      var entity = await AssertDbContext.TestEntities.SingleAsync();

      entity.ConvertibleClass.Should().NotBeNull();
      entity.ConvertibleClass!.Key.Should().Be(42);
   }

   [Fact]
   public async Task Should_insert_private_property()
   {
      var testEntity = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" };
      testEntity.SetPrivateField(3);

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity!.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_flags_enum()
   {
      var testEntity = new TestEntityWithFlags
                       {
                          Id = _TEST_ENTITY_ID,
                          GroupId = 1,
                          Phase = PhaseMembership.A | PhaseMembership.C
                       };

      await SUT.BulkInsertAsync(new[] { testEntity }, new MySqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      var testEntity = new TestEntityWithShadowProperties { Id = _TEST_ENTITY_ID };
      ActDbContext.Entry(testEntity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(testEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      AssertDbContext.Entry(loadedEntity!).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity!).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_specified_properties_only(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      options.PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties());
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42,
                          PropertyWithBackingField = 7
                       };
      testEntity.SetPrivateField(3);

      await SUT.BulkInsertAsync([testEntity], options);

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
   public async Task Should_accept_options_of_other_providers()
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };

      var affectedRows = await SUT.BulkInsertAsync([testEntity],
                                                   new GenericBulkOperationOptions
                                                   {
                                                      PropertiesToInsert = IEntityPropertiesProvider.Include(TestEntity.GetRequiredProperties())
                                                   });

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Name = null,
                                              RequiredName = "RequiredName",
                                              Count = 42
                                           });
   }

   [Fact]
   public async Task Should_insert_properties_of_base_class()
   {
      var testEntity = new TestEntityWithBaseClass { Id = _TEST_ENTITY_ID, Name = "Name" };

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithBaseClass.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_keyless_entities()
   {
      var affectedRows = await SUT.BulkInsertAsync([new KeylessTestEntity { IntColumn = 1 }, new KeylessTestEntity { IntColumn = 2 }], new MySqlBulkInsertOptions());

      affectedRows.Should().Be(2);

      var loadedValues = await AssertDbContext.KeylessEntities.Select(e => e.IntColumn).OrderBy(i => i).ToListAsync();
      loadedValues.Should().Equal(1, 2);
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

      var affectedRows = await SUT.BulkInsertAsync(entities, options);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().BeEquivalentTo(entities, config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_row_that_references_another_row_of_the_same_batch()
   {
      var parent = new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Parent" };
      var child = new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Child", ParentId = _TEST_ENTITY_ID };

      // MySQL validates foreign keys row by row, so the parent has to come first.
      await SUT.BulkInsertAsync([parent, child], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.RequiredName).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Id.Should().Be(_OTHER_TEST_ENTITY_ID);
      loadedEntities[0].ParentId.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[1].Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[1].ParentId.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_auto_increment_column()
   {
      var testEntity = new TestEntityWithAutoIncrement { Name = "value" };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions
                                              {
                                                 PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name)
                                              });

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.Id.Should().NotBe(0);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_insert_explicit_value_into_auto_increment_column()
   {
      var testEntity = new TestEntityWithAutoIncrement { Id = 42, Name = "value" };

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.SingleAsync();
      loadedEntity.Id.Should().Be(42);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_let_mysql_generate_value_if_zero_is_inserted_into_auto_increment_column()
   {
      var testEntity = new TestEntityWithAutoIncrement { Id = 0, Name = "value" };

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      // Without the SQL mode NO_AUTO_VALUE_ON_ZERO, MySQL treats 0 like NULL and generates the next value.
      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.SingleAsync();
      loadedEntity.Id.Should().NotBe(0);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_throw_because_mysql_dont_use_sql_default_value_for_null_in_NOT_NULL_column()
   {
      var testEntity = new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, String = null! };

      // 1048: column cannot be null
      var exception = await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1048);
   }

   [Fact]
   public async Task Should_write_provided_column_values_as_is_despite_sql_default_value()
   {
      var testEntity = new TestEntityWithSqlDefaultValues
                       {
                          Id = Guid.Empty,
                          Int = 0,
                          String = null!,
                          NullableInt = null,
                          NullableString = null
                       };

      var options = new MySqlBulkInsertOptions
                    {
                       // we skip TestEntityWithSqlDefaultValues.String
                       PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => new
                                                                                                                   {
                                                                                                                      e.Id,
                                                                                                                      e.Int,
                                                                                                                      e.NullableInt,
                                                                                                                      e.NullableString
                                                                                                                   })
                    };

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                           {
                                              Id = Guid.Empty,      // persisted as-is
                                              Int = 0,              // persisted as-is
                                              NullableInt = null,   // persisted as-is
                                              String = "3",         // DEFAULT value constraint
                                              NullableString = null // persisted as-is
                                           });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_use_sql_default_values_of_columns_that_are_not_inserted(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      options.PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Id);
      var testEntity = new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID };

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Int = 1,
                                              NullableInt = 2,
                                              String = "3",
                                              NullableString = "4"
                                           });
   }

   [Fact]
   public async Task Should_use_sql_default_expression_for_primary_key_that_is_not_inserted()
   {
      var testEntity = new TestEntityWithSqlDefaultValues { Int = 42 };

      await SUT.BulkInsertAsync([testEntity],
                                new MySqlBulkInsertOptions
                                {
                                   PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithSqlDefaultValues>(e => e.Int)
                                });

      // The default of the column is (UUID()).
      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Id.Should().NotBe(Guid.Empty);
      loadedEntity.Int.Should().Be(42);
   }

   [Fact]
   public async Task Should_throw_because_mysql_dont_use_dotnet_default_value_for_null_in_NOT_NULL_column()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues { Id = _TEST_ENTITY_ID, String = null! };

      // 1048: column cannot be null
      var exception = await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1048);
   }

   [Fact]
   public async Task Should_write_provided_column_values_as_is_despite_dotnet_default_value()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues
                       {
                          Id = Guid.Empty,
                          Int = 0,
                          String = null!,
                          NullableInt = null,
                          NullableString = null
                       };

      var options = new MySqlBulkInsertOptions
                    {
                       // we skip TestEntityWithDotnetDefaultValues.String
                       PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithDotnetDefaultValues>(e => new
                                                                                                                      {
                                                                                                                         e.Id,
                                                                                                                         e.Int,
                                                                                                                         e.NullableInt,
                                                                                                                         e.NullableString
                                                                                                                      })
                    };

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDotnetDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithDotnetDefaultValues
                                           {
                                              Id = Guid.Empty,      // persisted as-is
                                              Int = 0,              // persisted as-is
                                              NullableInt = null,   // persisted as-is
                                              String = "3",         // DEFAULT value constraint
                                              NullableString = null // persisted as-is
                                           });
   }

   [Fact]
   public async Task Should_use_dotnet_default_values_of_columns_that_are_not_inserted()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues();

      await SUT.BulkInsertAsync([testEntity],
                                new MySqlBulkInsertOptions
                                {
                                   PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithDotnetDefaultValues>(e => e.Int)
                                });

      var loadedEntity = await AssertDbContext.TestEntitiesWithDotnetDefaultValues.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithDotnetDefaultValues
                                           {
                                              Id = new Guid("0B151271-79BB-4F6C-B85F-E8F61300FF1B"),
                                              Int = 0,
                                              NullableInt = 2,
                                              String = "3",
                                              NullableString = "4"
                                           });
   }

   [Fact]
   public async Task Should_insert_inlined_owned_types()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = _INLINE_OWNER_ID,
                          InlineEntity = new OwnedEntity
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value"
                                         }
                       };

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity_Owns_Inline
                                           {
                                              Id = _INLINE_OWNER_ID,
                                              InlineEntity = new OwnedEntity
                                                             {
                                                                IntColumn = 42,
                                                                StringColumn = "value"
                                                             }
                                           });
   }

   [Fact]
   public async Task Should_throw_if_no_property_is_selected_for_insert()
   {
      // MySQL could insert rows from the defaults alone ("INSERT INTO ... () VALUES ()"), but the entity data reader shared by all providers rejects an empty selection.
      await SUT.Awaiting(sut => sut.BulkInsertAsync([new TestEntityWithAutoIncrement()],
                                                    new MySqlBulkInsertOptions
                                                    {
                                                       PropertiesToInsert = IEntityPropertiesProvider.Exclude<TestEntityWithAutoIncrement>(e => new { e.Id, e.Name })
                                                    }))
               .Should().ThrowAsync<ArgumentException>()
               .WithMessage("The properties collection cannot be empty*");

      (await AssertDbContext.TestEntitiesWithAutoIncrement.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_if_required_inlined_owned_type_is_null()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = _INLINE_OWNER_ID,
                          InlineEntity = null!
                       };

      // 1048: the column of the required owned property cannot be null
      var exception = await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1048);

      (await AssertDbContext.TestEntities_Own_Inline.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _SEPARATE_OWNER_ID,
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1"
                             },

                             new()
                             {
                                IntColumn = 43,
                                StringColumn = "value 2"
                             },
                          ]
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Theory]
   [InlineData(null)]
   [InlineData(2)]
   public async Task Should_insert_multiple_owners_with_and_without_separate_owned_entity(int? batchSize)
   {
      var testEntities = Enumerable.Range(1, 5)
                                   .Select(i => new TestEntity_Owns_SeparateOne
                                                {
                                                   Id = Guid.NewGuid(),
                                                   SeparateEntity = i % 2 == 0 ? null : new OwnedEntity { IntColumn = i, StringColumn = $"value {i}" }
                                                })
                                   .ToList();
      ActDbContext.AddRange(testEntities);

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions { BatchSize = batchSize });

      affectedRows.Should().Be(8, "5 owners and 3 owned entities");

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(testEntities);
   }

   [Theory]
   [InlineData(null)]
   [InlineData(2)]
   public async Task Should_insert_multiple_owners_with_separate_owned_collections(int? batchSize)
   {
      var testEntities = new[]
                         {
                            new TestEntity_Owns_SeparateMany { Id = Guid.NewGuid(), SeparateEntities = [] },
                            new TestEntity_Owns_SeparateMany { Id = Guid.NewGuid(), SeparateEntities = [new() { IntColumn = 1, StringColumn = "value 1" }] },
                            new TestEntity_Owns_SeparateMany
                            {
                               Id = Guid.NewGuid(),
                               SeparateEntities = [new() { IntColumn = 2, StringColumn = "value 2" }, new() { IntColumn = 3, StringColumn = null }, new() { IntColumn = 4, StringColumn = "value 4" }]
                            }
                         };
      ActDbContext.AddRange(testEntities);

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions { BatchSize = batchSize });

      affectedRows.Should().Be(7, "3 owners and 4 owned entities");

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(testEntities);
   }

   [Fact]
   public async Task Should_insert_inlined_owned_type_if_it_has_default_values_only()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = _INLINE_OWNER_ID,
                          InlineEntity = new OwnedEntity()
                       };

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity_Owns_Inline
                                           {
                                              Id = _INLINE_OWNER_ID,
                                              InlineEntity = new OwnedEntity
                                                             {
                                                                IntColumn = 0,
                                                                StringColumn = null
                                                             }
                                           });
   }

   [Fact]
   public async Task Should_throw_if_separated_owned_type_uses_shadow_property_id_and_is_detached()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _SEPARATE_OWNER_ID,
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");

      // The executor runs in its own transaction, so the owner row is rolled back.
      (await AssertDbContext.TestEntities_Own_SeparateOne.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_if_separated_owned_types_uses_shadow_property_id_and_is_detached()
   {
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1"
                             },
                          ]
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");

      // The executor runs in its own transaction, so the owner row is rolled back.
      (await AssertDbContext.TestEntities_Own_SeparateMany.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_Inline()
   {
      var testEntity = new TestEntity_Owns_Inline_Inline
                       {
                          Id = _NESTED_OWNER_ID,
                          InlineEntity = new OwnedEntity_Owns_Inline
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value 1",
                                            InlineEntity = new OwnedEntity
                                                           {
                                                              IntColumn = 43,
                                                              StringColumn = "value 2"
                                                           }
                                         }
                       };
      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_Inline_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          InlineEntity = new OwnedEntity_Owns_SeparateMany
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value 1",
                                            SeparateEntities =
                                            [
                                               new()
                                               {
                                                  IntColumn = 43,
                                                  StringColumn = "value 2"
                                               },

                                               new()
                                               {
                                                  IntColumn = 44,
                                                  StringColumn = "value 3"
                                               },
                                            ]
                                         }
                       };
      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_Inline_SeparateOne
                       {
                          Id = _NESTED_OWNER_ID,
                          InlineEntity = new OwnedEntity_Owns_SeparateOne
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value 1",
                                            SeparateEntity = new()
                                                             {
                                                                IntColumn = 43,
                                                                StringColumn = "value 2"
                                                             }
                                         }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany_Inline()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_Inline
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1",
                                InlineEntity = new OwnedEntity
                                               {
                                                  IntColumn = 43,
                                                  StringColumn = "value 2"
                                               }
                             },

                             new()
                             {
                                IntColumn = 44,
                                StringColumn = "value 3",
                                InlineEntity = new OwnedEntity
                                               {
                                                  IntColumn = 45,
                                                  StringColumn = "value 4"
                                               }
                             },
                          ]
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1",
                                SeparateEntities =
                                [
                                   new()
                                   {
                                      IntColumn = 43,
                                      StringColumn = "value 2"
                                   },

                                   new()
                                   {
                                      IntColumn = 44,
                                      StringColumn = "value 3"
                                   },
                                ]
                             },

                             new()
                             {
                                IntColumn = 45,
                                StringColumn = "value 4",
                                SeparateEntities =
                                [
                                   new()
                                   {
                                      IntColumn = 46,
                                      StringColumn = "value 5"
                                   },

                                   new()
                                   {
                                      IntColumn = 47,
                                      StringColumn = "value 6"
                                   },
                                ]
                             },
                          ]
                       };

      ActDbContext.Add(testEntity);

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities#OwnedEntity_Owns_SeparateMany.SeparateEntities' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities' is not supported.");

      (await AssertDbContext.TestEntities_Own_SeparateMany_SeparateMany.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateOne
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1",
                                SeparateEntity = new()
                                                 {
                                                    IntColumn = 43,
                                                    StringColumn = "value 2"
                                                 }
                             },

                             new()
                             {
                                IntColumn = 45,
                                StringColumn = "value 4",
                                SeparateEntity = new()
                                                 {
                                                    IntColumn = 46,
                                                    StringColumn = "value 5"
                                                 }
                             },
                          ]
                       };

      ActDbContext.Add(testEntity);

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities#OwnedEntity_Owns_SeparateOne.SeparateEntity' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities' is not supported.");

      (await AssertDbContext.TestEntities_Own_SeparateMany_SeparateOne.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_Inline()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_Inline
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntity = new()
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value 1",
                                              InlineEntity = new OwnedEntity
                                                             {
                                                                IntColumn = 43,
                                                                StringColumn = "value 2"
                                                             }
                                           }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntity = new()
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value 1",
                                              SeparateEntities =
                                              [
                                                 new()
                                                 {
                                                    IntColumn = 43,
                                                    StringColumn = "value 2"
                                                 },

                                                 new()
                                                 {
                                                    IntColumn = 44,
                                                    StringColumn = "value 3"
                                                 },
                                              ]
                                           }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_SeparateOne
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntity = new()
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value 1",
                                              SeparateEntity = new()
                                                               {
                                                                  IntColumn = 43,
                                                                  StringColumn = "value 2"
                                                               }
                                           }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_multiple_owners_with_nested_separate_owned_entities()
   {
      var testEntities = new[]
                         {
                            new TestEntity_Owns_SeparateOne_SeparateMany
                            {
                               Id = Guid.NewGuid(),
                               SeparateEntity = new()
                                                {
                                                   IntColumn = 1,
                                                   StringColumn = "value 1",
                                                   SeparateEntities = [new() { IntColumn = 2, StringColumn = "value 2" }, new() { IntColumn = 3, StringColumn = "value 3" }]
                                                }
                            },
                            new TestEntity_Owns_SeparateOne_SeparateMany
                            {
                               Id = Guid.NewGuid(),
                               SeparateEntity = new()
                                                {
                                                   IntColumn = 4,
                                                   StringColumn = "value 4",
                                                   SeparateEntities = [new() { IntColumn = 5, StringColumn = "value 5" }]
                                                }
                            }
                         };
      ActDbContext.AddRange(testEntities);

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new MySqlBulkInsertOptions { BatchSize = 1 });

      affectedRows.Should().Be(7, "2 owners, 2 owned entities and 3 nested owned entities");

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(testEntities);
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(_NESTED_OWNER_ID, new BoundaryValueObject(2, 5));

      await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_entity_with_json_column(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var entity = new TestEntityWithJsonColumns
                   {
                      Id = _TEST_ENTITY_ID,
                      JsonColumn = """{"key": "value"}"""
                   };

      var affectedRows = await SUT.BulkInsertAsync([entity], options);

      affectedRows.Should().Be(1);

      // MySQL normalizes the JSON text; the value is already in the normalized form.
      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.JsonColumn.Should().Be("""{"key": "value"}""");
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_entity_with_null_json_column(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      var entity = new TestEntityWithJsonColumns
                   {
                      Id = _TEST_ENTITY_ID,
                      JsonColumn = null
                   };

      var affectedRows = await SUT.BulkInsertAsync([entity], options);

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntity.JsonColumn.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_into_table_name_override()
   {
      await CreateTableCopyAsync("TestEntities", "TestEntities_BulkInsertRedirect");

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = _TEST_ENTITY_ID,
                             Name = "Name",
                             RequiredName = "RequiredName",
                             Count = 42
                          };

         await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableName = "TestEntities_BulkInsertRedirect" });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await LoadColumnAsync<Guid>("TestEntities_BulkInsertRedirect", "Id");
         redirectedIds.Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkInsertRedirect");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntity_provider()
   {
      await CreateTableCopyAsync("TestEntities", "TestEntities_BulkInsertRedirect");

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = _TEST_ENTITY_ID,
                             Name = "Name",
                             RequiredName = "RequiredName",
                             Count = 42
                          };

         await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect") });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await LoadColumnAsync<Guid>("TestEntities_BulkInsertRedirect", "Id");
         redirectedIds.Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkInsertRedirect");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_of_the_database_from_ForRootEntity_provider()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database.
      var database = await ActDbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() AS `Value`").SingleAsync();
      await CreateTableCopyAsync("TestEntities", "TestEntities_BulkInsertRedirect");

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = _TEST_ENTITY_ID,
                             RequiredName = "RequiredName"
                          };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect", database) });

         affectedRows.Should().Be(1);
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
         (await LoadColumnAsync<Guid>("TestEntities_BulkInsertRedirect", "Id")).Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkInsertRedirect");
      }
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_escape_table_name_of_table_name_provider(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      const string tableName = "TestEntities`; DROP TABLE TestEntities; -- Redirect";
      await CreateTableCopyAsync("TestEntities", tableName);

      try
      {
         options.TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tableName);

         var affectedRows = await SUT.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }], options);

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<Guid>(tableName, "Id")).Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync(tableName);
      }
   }

   // https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/issues/76
   // The owned entity must not be written to the table of the model. The bulk insert fails when it reaches the owned type.
   // The executor runs in its own transaction, so (unlike with PostgreSQL) the owner row is rolled back as well.
   [Fact]
   public async Task Should_not_insert_separated_owned_entities_into_model_table_when_table_name_is_overridden()
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateOne", "TestEntities_Own_SeparateOne_BulkInsertRedirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne
                          {
                             Id = _SEPARATE_OWNER_ID,
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);

         await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne_BulkInsertRedirect" }))
                  .Should().ThrowAsync<InvalidOperationException>()
                  .WithMessage("*override the table of the root entity only*");

         (await LoadColumnAsync<Guid>("SeparateEntitiesOne", "TestEntity_Owns_SeparateOneId"))
            .Should().BeEmpty("the owned entity must not be written to the table of the model when the owner is redirected");
         (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne_BulkInsertRedirect", "Id"))
            .Should().BeEmpty("the executor rolls back the owner row because it runs in its own transaction");
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_BulkInsertRedirect");
      }
   }

   [Fact]
   public async Task Should_throw_when_table_name_is_overridden_and_owned_entities_have_separate_tables()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _SEPARATE_OWNER_ID,
                          SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                       };
      ActDbContext.Add(testEntity);

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");

      // The executor runs in its own transaction, so the root row is rolled back when the operation reaches the owned type.
      (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne", "Id")).Should().BeEmpty();
      (await LoadColumnAsync<int>("SeparateEntitiesOne", "IntColumn")).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_leave_rollback_to_the_caller_if_owned_entity_fails_inside_user_transaction()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _SEPARATE_OWNER_ID,
                          SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                       };
      ActDbContext.Add(testEntity);
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");

      // The executor never ends a transaction of the caller. The caller decides about the root row.
      (await ActDbContext.Database.SqlQueryRaw<Guid>("SELECT `Id` AS `Value` FROM `TestEntities_Own_SeparateOne`").ToListAsync()).Should().Equal(_SEPARATE_OWNER_ID);

      await transaction.RollbackAsync();

      (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne", "Id")).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_use_table_name_provider_when_it_is_set_after_table_name()
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          RequiredName = "RequiredName"
                       };
      var provider = new SuffixTableNameProvider("");

      var options = new MySqlBulkInsertOptions
                    {
                       TableName = "TestEntities",
                       TableNameProvider = provider
                    };

      options.TableNameProvider.Should().BeSameAs(provider);

      await SUT.BulkInsertAsync([testEntity], options);

      provider.RequestedEntityTypes.Should().ContainSingle();
      (await AssertDbContext.TestEntities.ToListAsync()).Should().HaveCount(1);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_root_and_owned_entity_into_tables_from_table_name_provider(bool useGenericOptions)
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateOne", "TestEntities_Own_SeparateOne_Redirect");
      await CreateTableCopyAsync("SeparateEntitiesOne", "SeparateEntitiesOne_Redirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne
                          {
                             Id = _SEPARATE_OWNER_ID,
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);
         var provider = new SuffixTableNameProvider("_Redirect");
         IBulkInsertOptions options = useGenericOptions
                                         ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                         : new MySqlBulkInsertOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(2);
         (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne_Redirect", "Id")).Should().BeEquivalentTo([testEntity.Id]);
         (await LoadColumnAsync<int>("SeparateEntitiesOne_Redirect", "IntColumn")).Should().BeEquivalentTo([42]);
         (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await LoadColumnAsync<int>("SeparateEntitiesOne", "IntColumn")).Should().BeEmpty();

         provider.RequestedEntityTypes.Should().HaveCount(2);
         provider.RequestedEntityTypes.Should().ContainSingle(t => t.FindOwnership() == null && t.ClrType == typeof(TestEntity_Owns_SeparateOne));
         provider.RequestedEntityTypes.Should().ContainSingle(t => t.FindOwnership() != null && t.FindOwnership()!.PrincipalToDependent!.Name == "SeparateEntity");
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_Redirect", "SeparateEntitiesOne_Redirect");
      }
   }

   [Fact]
   public async Task Should_insert_nested_owned_entities_into_tables_from_table_name_provider()
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateOne_SeparateOne", "TestEntities_Own_SeparateOne_SeparateOne_Redirect");
      await CreateTableCopyAsync("SeparateEntitiesOne_SeparateOne", "SeparateEntitiesOne_SeparateOne_Redirect");
      await CreateTableCopyAsync("SeparateEntitiesOne_SeparateOne_Inner", "SeparateEntitiesOne_SeparateOne_Inner_Redirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne_SeparateOne
                          {
                             Id = _NESTED_OWNER_ID,
                             SeparateEntity = new()
                                              {
                                                 IntColumn = 42,
                                                 StringColumn = "value 1",
                                                 SeparateEntity = new() { IntColumn = 43, StringColumn = "value 2" }
                                              }
                          };
         ActDbContext.Add(testEntity);
         var provider = new SuffixTableNameProvider("_Redirect");

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableNameProvider = provider });

         affectedRows.Should().Be(3);
         (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne_SeparateOne_Redirect", "Id")).Should().BeEquivalentTo([testEntity.Id]);
         (await LoadColumnAsync<int>("SeparateEntitiesOne_SeparateOne_Redirect", "IntColumn")).Should().BeEquivalentTo([42]);
         (await LoadColumnAsync<int>("SeparateEntitiesOne_SeparateOne_Inner_Redirect", "IntColumn")).Should().BeEquivalentTo([43]);
         (await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await LoadColumnAsync<int>("SeparateEntitiesOne_SeparateOne", "IntColumn")).Should().BeEmpty();
         (await LoadColumnAsync<int>("SeparateEntitiesOne_SeparateOne_Inner", "IntColumn")).Should().BeEmpty();

         provider.RequestedEntityTypes.Should().HaveCount(3).And.OnlyHaveUniqueItems();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_SeparateOne_Redirect", "SeparateEntitiesOne_SeparateOne_Redirect", "SeparateEntitiesOne_SeparateOne_Inner_Redirect");
      }
   }

   [Fact]
   public async Task Should_insert_owned_collection_into_table_from_table_name_provider()
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateMany", "TestEntities_Own_SeparateMany_Redirect");
      await CreateTableCopyAsync("SeparateEntitiesMany", "SeparateEntitiesMany_Redirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateMany
                          {
                             Id = _NESTED_OWNER_ID,
                             SeparateEntities =
                             [
                                new() { IntColumn = 1, StringColumn = "value 1" },
                                new() { IntColumn = 2, StringColumn = "value 2" }
                             ]
                          };
         ActDbContext.Add(testEntity);

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new MySqlBulkInsertOptions { TableNameProvider = new SuffixTableNameProvider("_Redirect") });

         affectedRows.Should().Be(3);
         (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateMany_Redirect", "Id")).Should().BeEquivalentTo([testEntity.Id]);
         (await LoadColumnAsync<int>("SeparateEntitiesMany_Redirect", "IntColumn")).Should().BeEquivalentTo([1, 2]);
         (await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync()).Should().BeEmpty();
         (await LoadColumnAsync<int>("SeparateEntitiesMany", "IntColumn")).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateMany_Redirect", "SeparateEntitiesMany_Redirect");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_name_override_when_separate_owned_entities_are_excluded()
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateOne", "TestEntities_Own_SeparateOne_Redirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne
                          {
                             Id = _SEPARATE_OWNER_ID,
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);

         var options = new MySqlBulkInsertOptions
                       {
                          TableName = "TestEntities_Own_SeparateOne_Redirect",
                          PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => e.Id)
                       };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<Guid>("TestEntities_Own_SeparateOne_Redirect", "Id")).Should().BeEquivalentTo([testEntity.Id]);
         (await LoadColumnAsync<int>("SeparateEntitiesOne", "IntColumn")).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_Redirect");
      }
   }

   [Fact]
   public async Task Should_insert_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntityTempTable
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new MySqlBulkInsertOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[0].Name.Should().Be("Name");
      loadedEntities[0].RequiredName.Should().Be("RequiredName");
      loadedEntities[0].Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_insert_temp_table_entity_with_ForRootEntity_provider()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntityTempTable
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities") });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(_TEST_ENTITY_ID);
      loadedEntities[0].Name.Should().Be("Name");
      loadedEntities[0].RequiredName.Should().Be("RequiredName");
      loadedEntities[0].Count.Should().Be(42);
   }

   // The tests below cover behavior specific to the MySQL executor: batching, command timeout, transactions, MySqlBulkCopy.

   [Fact]
   public async Task Should_return_zero_for_empty_input_without_sending_statements()
   {
      await OpenActConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertAsync(Array.Empty<TestEntity>(), new MySqlBulkInsertOptions());
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(0);
      statements.Should().Be(0);
   }

   [Fact]
   public async Task Should_return_zero_for_empty_input_with_bulk_copy()
   {
      var options = CreateOptions(true);

      var affectedRows = await SUT.BulkInsertAsync(Array.Empty<TestEntity>(), options);

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
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
      var entities = Enumerable.Range(0, rowCount).Select(CreateTestEntity);
      await OpenActConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertAsync(entities, new MySqlBulkInsertOptions { BatchSize = batchSize });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(rowCount);
      statements.Should().Be(expectedStatements);

      var loadedCounts = await AssertDbContext.TestEntities.Select(e => e.Count).OrderBy(c => c).ToListAsync();
      loadedCounts.Should().Equal(Enumerable.Range(0, rowCount));
   }

   [Fact]
   public async Task Should_limit_rows_per_statement_to_the_parameter_limit_of_mysql()
   {
      // MySQL accepts 65,535 parameters per statement, so the number of rows per statement is limited by the number of columns.
      var columnCount = ActDbContext.Model.FindEntityType(typeof(TestEntity))!.GetProperties().Count();
      var maximumRows = 65_535 / columnCount;
      var entities = Enumerable.Range(0, maximumRows + 1).Select(CreateTestEntity);
      await OpenActConnectionAsync();

      var statementsBefore = await GetSessionStatusAsync("Com_insert");
      var affectedRows = await SUT.BulkInsertAsync(entities, new MySqlBulkInsertOptions { BatchSize = Int32.MaxValue });
      var statements = await GetSessionStatusAsync("Com_insert") - statementsBefore;

      affectedRows.Should().Be(maximumRows + 1);
      statements.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(maximumRows + 1);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   public async Task Should_throw_if_batch_size_is_not_positive(int batchSize)
   {
      await SUT.Awaiting(sut => sut.BulkInsertAsync([CreateTestEntity(1)], new MySqlBulkInsertOptions { BatchSize = batchSize }))
               .Should().ThrowAsync<ArgumentOutOfRangeException>()
               .WithMessage("*batch size*greater than 0*");

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(false, 500)]
   [InlineData(false, 30_000)]
   [InlineData(true, 500)]
   [InlineData(true, 30_000)]
   public async Task Should_insert_entities_with_command_timeout(bool useBulkCopy, int timeoutInMilliseconds)
   {
      var options = CreateOptions(useBulkCopy);
      options.CommandTimeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

      var affectedRows = await SUT.BulkInsertAsync([CreateTestEntity(1), CreateTestEntity(2)], options);

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(2);
   }

   [Fact]
   public async Task Should_apply_command_timeout_to_insert_statements()
   {
      // The uncommitted row of another transaction blocks the insert of the same primary key
      // until the lock wait timeout of the server (50 seconds by default) is reached.
      await using var blockingTransaction = await ArrangeDbContext.Database.BeginTransactionAsync();
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Blocking" });
      await ArrangeDbContext.SaveChangesAsync();

      var stopwatch = Stopwatch.StartNew();

      await SUT.Awaiting(sut => sut.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Blocked" }],
                                                    new MySqlBulkInsertOptions { CommandTimeout = TimeSpan.FromSeconds(1) }))
               .Should().ThrowAsync<MySqlException>();

      stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
   }

   [Fact]
   public async Task Should_throw_when_cancellation_is_requested()
   {
      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      await SUT.Awaiting(sut => sut.BulkInsertAsync([CreateTestEntity(1)], new MySqlBulkInsertOptions(), cancellationTokenSource.Token))
               .Should().ThrowAsync<OperationCanceledException>();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_leave_partial_rows_when_a_later_batch_fails()
   {
      var entities = new[]
                     {
                        new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "First" },
                        new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "Second" },
                        new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Duplicate of the first row" }
                     };

      // 1062: duplicate primary key in the third batch
      var exception = await SUT.Awaiting(sut => sut.BulkInsertAsync(entities, new MySqlBulkInsertOptions { BatchSize = 1 }))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_leave_rows_of_a_complete_batch_when_the_last_batch_fails()
   {
      // The first statement contains 500 valid rows (default batch size), the duplicate is the only row of the second statement.
      var entities = Enumerable.Range(0, 500).Select(CreateTestEntity).ToList();
      entities.Add(new TestEntity { Id = entities[0].Id, RequiredName = "Duplicate", Count = 500 });

      var exception = await SUT.Awaiting(sut => sut.BulkInsertAsync(entities, new MySqlBulkInsertOptions()))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_not_end_the_transaction_of_the_caller_and_commit_rows_with_it(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkInsertAsync([CreateTestEntity(1), CreateTestEntity(2)], options);

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.CommitAsync();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(2);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_not_insert_rows_if_the_transaction_of_the_caller_is_rolled_back(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkInsertAsync([CreateTestEntity(1), CreateTestEntity(2)], options);

      affectedRows.Should().Be(2);

      await transaction.RollbackAsync();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_rows_into_the_nested_transaction_of_the_caller(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      await using var rootTransaction = await ActDbContext.Database.BeginTransactionAsync();
      await using var nestedTransaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await SUT.BulkInsertAsync([CreateTestEntity(1), CreateTestEntity(2)], options);

      affectedRows.Should().Be(2);

      // A nested transaction is logical only; the root transaction decides about the rows.
      await nestedTransaction.CommitAsync();
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);

      await rootTransaction.RollbackAsync();
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntity_provider_with_bulk_copy()
   {
      var options = CreateOptions(true);
      await CreateTableCopyAsync("TestEntities", "TestEntities_BulkInsertRedirect");

      try
      {
         options.TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect");

         var affectedRows = await SUT.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }], options);

         affectedRows.Should().Be(1);
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
         (await LoadColumnAsync<Guid>("TestEntities_BulkInsertRedirect", "Id")).Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync("TestEntities_BulkInsertRedirect");
      }
   }

   [Fact]
   public async Task Should_insert_entities_with_bulk_copy()
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
                               Name = null,
                               RequiredName = "RequiredName",
                               Count = 0,
                               NullableCount = null,
                               ConvertibleClass = null,
                               ParentId = null
                            };

      var affectedRows = await SUT.BulkInsertAsync([entityWithValues, entityWithNulls], options);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Should().BeEquivalentTo(entityWithNulls);
      loadedEntities[1].Should().BeEquivalentTo(entityWithValues);
      loadedEntities[1].GetPrivateField().Should().Be(9);
   }

   [Fact]
   public async Task Should_insert_all_rows_with_single_load_data_statement_and_ignore_batch_size()
   {
      var options = CreateOptions(true);
      options.BatchSize = 1;
      await OpenActConnectionAsync();

      var insertsBefore = await GetSessionStatusAsync("Com_insert");
      var loadsBefore = await GetSessionStatusAsync("Com_load");
      var affectedRows = await SUT.BulkInsertAsync(Enumerable.Range(0, 1001).Select(CreateTestEntity), options);
      var inserts = await GetSessionStatusAsync("Com_insert") - insertsBefore;
      var loads = await GetSessionStatusAsync("Com_load") - loadsBefore;

      affectedRows.Should().Be(1001);
      inserts.Should().Be(0);
      loads.Should().Be(1);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(1001);
   }

   [Fact]
   public async Task Should_insert_flags_enum_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntityWithFlags
                       {
                          Id = _TEST_ENTITY_ID,
                          GroupId = 1,
                          Phase = PhaseMembership.A | PhaseMembership.C
                       };

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.SingleAsync();
      loadedEntity.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
   }

   [Fact]
   public async Task Should_insert_shadow_properties_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntityWithShadowProperties { Id = _TEST_ENTITY_ID };
      ActDbContext.Entry(testEntity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(testEntity).Property("ShadowIntProperty").CurrentValue = null;

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.SingleAsync();
      AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_explicit_value_into_auto_increment_column_with_bulk_copy()
   {
      var options = CreateOptions(true);

      await SUT.BulkInsertAsync([new TestEntityWithAutoIncrement { Id = 42, Name = "value" }], options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.SingleAsync();
      loadedEntity.Id.Should().Be(42);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_insert_inlined_owned_types_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = _INLINE_OWNER_ID,
                          InlineEntity = new OwnedEntity
                                         {
                                            IntColumn = 42,
                                            StringColumn = null
                                         }
                       };

      await SUT.BulkInsertAsync([testEntity], options);

      var loadedEntity = await AssertDbContext.TestEntities_Own_Inline.SingleAsync();
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = _SEPARATE_OWNER_ID,
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntities_Own_SeparateOne.SingleAsync()).Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = _NESTED_OWNER_ID,
                          SeparateEntities =
                          [
                             new() { IntColumn = 42, StringColumn = "value 1" },
                             new() { IntColumn = 43, StringColumn = null }
                          ]
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

      affectedRows.Should().Be(3);
      (await AssertDbContext.TestEntities_Own_SeparateMany.SingleAsync()).Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType_with_bulk_copy()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntityWithComplexType(_NESTED_OWNER_ID, new BoundaryValueObject(2, 5));

      await SUT.BulkInsertAsync([testEntity], options);

      (await AssertDbContext.TestEntities_with_ComplexType.SingleAsync()).Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_throw_and_not_leave_rows_if_bulk_copy_writes_null_into_NOT_NULL_column()
   {
      var options = CreateOptions(true);
      var testEntity = new TestEntityWithSqlDefaultValues { Id = _TEST_ENTITY_ID, String = null! };

      // LOAD DATA LOCAL implies IGNORE, i.e. MySQL replaces the NULL by '' and reports a warning instead of an error.
      // The executor turns the warning into an exception and rolls back the rows of its own transaction.
      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], options))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("MySqlBulkCopy inserted*");

      (await AssertDbContext.TestEntitiesWithDefaultValues.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_strings_with_special_characters(bool useBulkCopy)
   {
      var options = CreateOptions(useBulkCopy);
      string?[] names =
      [
         null,
         "",
         "  padded  ",
         "O'Brien",
         "say \"hi\"",
         "back\\slash",
         "trailing backslash\\",
         "\\N",
         "NULL",
         "line1\nline2",
         "line1\r\nline2",
         "tab\tseparated",
         "`backtick`",
         "100%_",
         "'; DROP TABLE TestEntities; --",
         "emoji \U0001F600, \u65E5\u672C\u8A9E"
      ];
      var entities = names.Select((name, index) => new TestEntity
                                                   {
                                                      Id = Guid.NewGuid(),
                                                      Name = name,
                                                      RequiredName = name ?? "(null)",
                                                      Count = index
                                                   })
                          .ToList();

      var affectedRows = await SUT.BulkInsertAsync(entities, options);

      affectedRows.Should().Be(names.Length);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal(names);
      loadedEntities.Select(e => e.RequiredName).Should().Equal(names.Select(name => name ?? "(null)"));
   }

   private static TestEntity CreateTestEntity(int count)
   {
      return new TestEntity
             {
                Id = Guid.NewGuid(),
                RequiredName = $"RequiredName {count}",
                Count = count
             };
   }

   // MySqlBulkCopy uses LOAD DATA LOCAL INFILE, which the connection has to allow explicitly (the server is started with --local-infile=1).
   // This must happen before the first use of the connection of ActDbContext.
   private MySqlBulkInsertOptions CreateOptions(bool useBulkCopy)
   {
      if (useBulkCopy)
      {
         var connectionString = new MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = true };
         ActDbContext.Database.SetConnectionString(connectionString.ConnectionString);
      }

      return new MySqlBulkInsertOptions { UseBulkCopy = useBulkCopy };
   }

   // The status counters are per session. The connection of ActDbContext stays open, so the executor and the status queries share the session.
   private async Task OpenActConnectionAsync()
   {
      await ActDbContext.Database.OpenConnectionAsync();
   }

   private async Task<long> GetSessionStatusAsync(string name)
   {
      await using var command = ActDbContext.Database.GetDbConnection().CreateCommand();
      command.CommandText = $"SHOW SESSION STATUS LIKE '{name}'";
      await using var reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();

      return Int64.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
   }

   private async Task<List<T>> LoadColumnAsync<T>(string tableName, string columnName)
   {
      return await AssertDbContext.Database.SqlQueryRaw<T>($"SELECT {Quote(columnName)} AS `Value` FROM {Quote(tableName)}").ToListAsync();
   }

   // LIKE copies the columns, the indexes and the primary key, but no foreign keys.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(targetTableName)} LIKE {Quote(sourceTableName)}");
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
