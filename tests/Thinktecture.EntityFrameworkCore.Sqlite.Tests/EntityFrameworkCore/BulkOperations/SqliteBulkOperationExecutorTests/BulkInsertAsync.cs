using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;
   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkInsertAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_throw_when_inserting_entities_without_creating_table_first()
   {
      ConfigureModel = builder => builder.Entity<CustomTempTable>().HasNoKey();
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new List<CustomTempTable>(), new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>().WithMessage("Error during bulk operation on table '\"CustomTempTable\"'. See inner exception for more details.");
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

      var testEntities = new[] { testEntity };

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

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
   public async Task Should_insert_flags_enum()
   {
      var testEntity = new TestEntityWithFlags
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          GroupId = 1,
                          Phase = PhaseMembership.A | PhaseMembership.C
                       };

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
   }

   [Fact]
   public async Task Should_insert_private_property()
   {
      var testEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      testEntity.SetPrivateField(3);

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      var testEntity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ActDbContext.Entry(testEntity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(testEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
   }

   [Fact]
   public async Task Should_throw_because_sqlite_dont_support_null_for_NOT_NULL_despite_sql_default_value()
   {
      var testEntity = new TestEntityWithSqlDefaultValues { String = null! };
      var testEntities = new[] { testEntity };

      await SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<SqliteException>()
               .WithMessage("SQLite Error 19: 'NOT NULL constraint failed: TestEntitiesWithDefaultValues.String'.");
   }

   [Fact]
   public async Task Should_write_all_provided_column_values_as_is_despite_sql_default_value()
   {
      var testEntity = new TestEntityWithSqlDefaultValues
                       {
                          Id = Guid.Empty,
                          Int = 0,
                          String = null!,
                          NullableInt = null,
                          NullableString = null
                       };
      var testEntities = new[] { testEntity };

      var options = new SqliteBulkInsertOptions
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

      await SUT.BulkInsertAsync(testEntities, options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.FirstOrDefaultAsync();
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithSqlDefaultValues
                                           {
                                              Id = Guid.Empty,      // persisted as-is
                                              Int = 0,              // persisted as-is
                                              NullableInt = null,   // persisted as-is
                                              String = "3",         // DEFAULT value constraint
                                              NullableString = null // persisted as-is
                                           });
   }

   [Fact]
   public async Task Should_throw_because_sqlite_dont_support_null_for_NOT_NULL_despite_dotnet_default_value()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues { String = null! };
      var testEntities = new[] { testEntity };

      await SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<SqliteException>()
               .WithMessage("SQLite Error 19: 'NOT NULL constraint failed: TestEntitiesWithDotnetDefaultValues.String'.");
   }

   [Fact]
   public async Task Should_write_all_provided_column_values_as_is_despite_dotnet_default_value()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues
                       {
                          Id = Guid.Empty,
                          Int = 0,
                          String = null!,
                          NullableInt = null,
                          NullableString = null
                       };
      var testEntities = new[] { testEntity };

      var options = new SqliteBulkInsertOptions
                    {
                       // we skip TestEntityWithDefaultValues.String
                       PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithDotnetDefaultValues>(e => new
                                                                                                                      {
                                                                                                                         e.Id,
                                                                                                                         e.Int,
                                                                                                                         e.NullableInt,
                                                                                                                         e.NullableString
                                                                                                                      })
                    };

      await SUT.BulkInsertAsync(testEntities, options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDotnetDefaultValues.FirstOrDefaultAsync();
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
   [InlineData(SqliteAutoIncrementBehavior.SetZeroToNull, 42, 42)]
   [InlineData(SqliteAutoIncrementBehavior.KeepValueAsIs, 42, 42)]
   [InlineData(SqliteAutoIncrementBehavior.SetZeroToNull, 0, 1)] // 1 because the DB is empty
   [InlineData(SqliteAutoIncrementBehavior.KeepValueAsIs, 0, 0)]
   public async Task Should_insert_0_to_auto_increment_column(SqliteAutoIncrementBehavior behavior, int id, int expectedId)
   {
      var testEntity = new TestEntityWithAutoIncrement { Id = id };
      var testEntities = new[] { testEntity };

      var options = new SqliteBulkInsertOptions { AutoIncrementBehavior = behavior };
      await SUT.BulkInsertAsync(testEntities, options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.Id.Should().Be(expectedId);
      loadedEntity.Name.Should().BeNull();
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

      await SUT.BulkInsertAsync(testEntities,
                                new SqliteBulkInsertOptions
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
   public async Task Should_throw_if_required_inlined_owned_type_is_null()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = null!
                       };
      var testEntities = new[] { testEntity };

      await ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync(testEntities))
                        .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_if_it_has_default_values_only()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = new OwnedEntity()
                       };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity_Owns_Inline
                                           {
                                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                              InlineEntity = new OwnedEntity
                                                             {
                                                                IntColumn = 0,
                                                                StringColumn = null
                                                             }
                                           });
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = new OwnedEntity
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value"
                                         }
                       };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity_Owns_Inline
                                           {
                                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                              InlineEntity = new OwnedEntity
                                                             {
                                                                IntColumn = 42,
                                                                StringColumn = "value"
                                                             }
                                           });
   }

   [Fact]
   public async Task Should_throw_if_separated_owned_type_uses_shadow_property_id_and_is_detached()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_throw_if_separated_owned_types_uses_shadow_property_id_and_is_detached()
   {
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1"
                             },
                          ]
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities =
                          [
                             new()
                             {
                                IntColumn = 42,
                                StringColumn = "value 1"
                             },
                          ]
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_Inline()
   {
      var testEntity = new TestEntity_Owns_Inline_Inline
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_Inline_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_Inline_SeparateOne
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([
         testEntity,
      ], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany_Inline()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_Inline
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities#OwnedEntity_Owns_SeparateMany.SeparateEntities' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities' is not supported.");
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateOne
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities#OwnedEntity_Owns_SeparateOne.SeparateEntity' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities' is not supported.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_Inline()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_Inline
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateOne_SeparateOne
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
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

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_round_trip_nullable_ComplexTypes_through_SaveChanges()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("8B6B7C9E-2C2C-4C9B-9C6D-3F3A2E1D0C01"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("8B6B7C9E-2C2C-4C9B-9C6D-3F3A2E1D0C02"));
      ArrangeDbContext.AddRange(withValues, withNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls]);
   }

   [Fact]
   public async Task Should_insert_into_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_BulkInsertRedirect" AS SELECT * FROM "TestEntities" WHERE 0
         """);

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             RequiredName = "RequiredName",
                             Count = 42
                          };

         await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableName = "TestEntities_BulkInsertRedirect" });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<string>("SELECT [Id] FROM [TestEntities_BulkInsertRedirect]")
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_BulkInsertRedirect]");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_BulkInsertRedirect" AS SELECT * FROM "TestEntities" WHERE 0
         """);

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             RequiredName = "RequiredName",
                             Count = 42
                          };

         await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect") });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<string>("SELECT [Id] FROM [TestEntities_BulkInsertRedirect]")
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_BulkInsertRedirect]");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntityWithDefaultSchema_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_BulkInsertRedirect" AS SELECT * FROM "TestEntities" WHERE 0
         """);

      try
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             RequiredName = "RequiredName",
                             Count = 42
                          };

         await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("TestEntities_BulkInsertRedirect") });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<string>("SELECT [Id] FROM [TestEntities_BulkInsertRedirect]")
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_BulkInsertRedirect]");
      }
   }

   // https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/issues/76
   // The owned entity must either be redirected together with its owner or the bulk insert must fail without writing anything.
   [Fact]
   public async Task Should_not_insert_separated_owned_entities_into_model_table_when_table_name_is_overridden()
   {
      // The table in the issue has no foreign key to the owner. Without enforcement, the foreign key of the test schema
      // behaves the same way. With enforcement, the insert into the model table fails and the transaction hides the bug.
      await ActDbContext.Database.OpenConnectionAsync();
      await ActDbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_Own_SeparateOne_BulkInsertRedirect" AS SELECT * FROM "TestEntities_Own_SeparateOne" WHERE 0
         """);

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne
                          {
                             Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);

         Exception? exception = null;

         try
         {
            await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne_BulkInsertRedirect" });
         }
         catch (Exception ex)
         {
            exception = ex;
         }

         var ownedEntityIdsInModelTable = await AssertDbContext.Database
                                                               .SqlQueryRaw<string>("SELECT [TestEntity_Owns_SeparateOneId] FROM [SeparateEntitiesOne]")
                                                               .ToListAsync();
         ownedEntityIdsInModelTable.Should().BeEmpty("the owned entity must not be written to the table of the model when the owner is redirected");

         if (exception is not null)
         {
            var ownerIds = await AssertDbContext.Database
                                                .SqlQueryRaw<string>("SELECT [Id] FROM [TestEntities_Own_SeparateOne_BulkInsertRedirect]")
                                                .ToListAsync();
            ownerIds.Should().BeEmpty($"a failed bulk insert must not leave the owner behind (exception: {exception.Message})");
         }
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS [TestEntities_Own_SeparateOne_BulkInsertRedirect]");
      }
   }

   [Fact]
   public async Task Should_throw_when_table_name_is_overridden_and_owned_entities_have_separate_tables()
   {
      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                       };
      ActDbContext.Add(testEntity);

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");

      (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
      (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_use_table_name_provider_when_it_is_set_after_table_name()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          RequiredName = "RequiredName"
                       };
      var provider = new SuffixTableNameProvider("");

      var options = new SqliteBulkInsertOptions
                    {
                       TableName = "TestEntities",
                       TableNameProvider = provider
                    };

      options.TableNameProvider.Should().BeSameAs(provider);

      await SUT.BulkInsertAsync([testEntity], options);

      provider.RequestedEntityTypes.Should().ContainSingle();
      (await AssertDbContext.TestEntities.ToListAsync()).Should().HaveCount(1);
   }

   [Fact]
   public async Task Should_throw_when_table_name_provider_returns_empty_schema()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          RequiredName = "RequiredName"
                       };

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = new EmptySchemaTableNameProvider() }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The 'TableNameProvider' returned an empty schema for the entity type '*TestEntity'. Return null to use the default schema of the database.");

      (await AssertDbContext.TestEntities.ToListAsync()).Should().BeEmpty();
   }

   private sealed class EmptySchemaTableNameProvider : IBulkOperationTableNameProvider
   {
      public BulkOperationTableName GetTableName(IEntityType entityType)
      {
         return new BulkOperationTableName("TestEntities", " ");
      }
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
                             Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);
         var provider = new SuffixTableNameProvider("_Redirect");
         IBulkInsertOptions options = useGenericOptions
                                         ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                         : new SqliteBulkInsertOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(2);
         (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Id" FROM "TestEntities_Own_SeparateOne_Redirect" """).ToListAsync())
            .Should().HaveCount(1);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();

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
   public async Task Should_insert_root_and_owned_entity_into_tables_from_created_table_name_provider()
   {
      await CreateTableCopyAsync("TestEntities_Own_SeparateOne", "TestEntities_Own_SeparateOne_Redirect");
      await CreateTableCopyAsync("SeparateEntitiesOne", "SeparateEntitiesOne_Redirect");

      try
      {
         var testEntity = new TestEntity_Owns_SeparateOne
                          {
                             Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);
         var provider = IBulkOperationTableNameProvider.Create(t => new BulkOperationTableName(IBulkOperationTableNameProvider.Default.GetTableName(t).Name + "_Redirect"));

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = provider });

         affectedRows.Should().Be(2);
         (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Id" FROM "TestEntities_Own_SeparateOne_Redirect" """).ToListAsync())
            .Should().HaveCount(1);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();
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
                             Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                             SeparateEntity = new()
                                              {
                                                 IntColumn = 42,
                                                 StringColumn = "value 1",
                                                 SeparateEntity = new() { IntColumn = 43, StringColumn = "value 2" }
                                              }
                          };
         ActDbContext.Add(testEntity);
         var provider = new SuffixTableNameProvider("_Redirect");

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = provider });

         affectedRows.Should().Be(3);
         (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Id" FROM "TestEntities_Own_SeparateOne_SeparateOne_Redirect" """).ToListAsync())
            .Should().HaveCount(1);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_SeparateOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_SeparateOne_Inner_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([43]);
         (await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_SeparateOne" """).ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne_SeparateOne_Inner" """).ToListAsync()).Should().BeEmpty();

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
                             Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                             // The key of the owned collection is not generated by SQLite, so a second item would have the same key.
                             SeparateEntities = [new() { IntColumn = 1, StringColumn = "value 1" }]
                          };
         ActDbContext.Add(testEntity);

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = new SuffixTableNameProvider("_Redirect") });

         affectedRows.Should().Be(2);
         (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Id" FROM "TestEntities_Own_SeparateMany_Redirect" """).ToListAsync())
            .Should().HaveCount(1);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesMany_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([1]);
         (await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesMany" """).ToListAsync()).Should().BeEmpty();
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
                             Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);

         var options = new SqliteBulkInsertOptions
                       {
                          TableName = "TestEntities_Own_SeparateOne_Redirect",
                          PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => e.Id)
                       };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(1);
         (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Id" FROM "TestEntities_Own_SeparateOne_Redirect" """).ToListAsync())
            .Should().HaveCount(1);
         (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_Redirect");
      }
   }

   [Fact]
   public async Task Should_resolve_distinct_tables_for_owned_types_with_same_clr_type()
   {
      ConfigureModel = TestEntity_Owns_TwoSeparateOnes.Configure;
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_Own_TwoSeparateOnes_Redirect" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NULL);
         CREATE TABLE "TwoSeparateOnes_First_Redirect" ("TestEntity_Owns_TwoSeparateOnesId" TEXT NOT NULL PRIMARY KEY, "StringColumn" TEXT NULL, "IntColumn" INTEGER NOT NULL);
         CREATE TABLE "TwoSeparateOnes_Second_Redirect" ("TestEntity_Owns_TwoSeparateOnesId" TEXT NOT NULL PRIMARY KEY, "StringColumn" TEXT NULL, "IntColumn" INTEGER NOT NULL);
         """);

      var testEntity = new TestEntity_Owns_TwoSeparateOnes
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          First = new OwnedEntity { IntColumn = 1 },
                          Second = new OwnedEntity { IntColumn = 2 }
                       };
      ActDbContext.Set<TestEntity_Owns_TwoSeparateOnes>().Add(testEntity);
      var provider = new SuffixTableNameProvider("_Redirect");

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions { TableNameProvider = provider });

      affectedRows.Should().Be(3);
      (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "TwoSeparateOnes_First_Redirect" """).ToListAsync()).Should().BeEquivalentTo([1]);
      (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "TwoSeparateOnes_Second_Redirect" """).ToListAsync()).Should().BeEquivalentTo([2]);

      provider.RequestedEntityTypes.Should().HaveCount(3).And.OnlyHaveUniqueItems();
      var ownedTypes = provider.RequestedEntityTypes.Where(t => t.ClrType == typeof(OwnedEntity)).ToList();
      ownedTypes.Should().HaveCount(2);
      ownedTypes.Select(t => t.Name).Should().OnlyHaveUniqueItems();
      ownedTypes.Select(t => t.FindOwnership()!.PrincipalToDependent!.Name).Should().BeEquivalentTo(["First", "Second"]);
   }

   // CREATE TABLE AS copies the columns only, without primary keys and foreign keys.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""CREATE TABLE "{targetTableName}" AS SELECT * FROM "{sourceTableName}" WHERE 0""");
   }

   private async Task DropTablesAsync(params string[] tableNames)
   {
      foreach (var tableName in tableNames)
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Invoking(sut => sut.BulkInsertAsync(new List<TempTable<int>> { new(0) }, new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_insert_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntityTempTable
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new SqliteBulkInsertOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[0].Name.Should().Be("Name");
      loadedEntities[0].RequiredName.Should().Be("RequiredName");
      loadedEntities[0].Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_insert_nullable_ComplexTypes_with_null_and_non_null_values()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("5D2F1B3A-0E5E-4F6C-9F35-7B2A6C1E0001"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("5D2F1B3A-0E5E-4F6C-9F35-7B2A6C1E0002"));
      var taggedWithoutMembers = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("5D2F1B3A-0E5E-4F6C-9F35-7B2A6C1E0003"));
      taggedWithoutMembers.Tagged = new TaggedValues();

      await SUT.BulkInsertAsync([withValues, withNulls, taggedWithoutMembers], new SqliteBulkInsertOptions());

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls, taggedWithoutMembers]);
   }

   [Fact]
   public async Task Should_throw_when_required_ComplexType_is_null()
   {
      var testEntity = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("5D2F1B3A-0E5E-4F6C-9F35-7B2A6C1E0004"));
      testEntity.Outer = null!;

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<SqliteException>()
               .WithMessage("SQLite Error 19: 'NOT NULL constraint failed: TestEntitiesWithNullableComplexTypes.Outer_Z'.");

      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_round_trip_nested_struct_ComplexTypes_through_SaveChanges()
   {
      var withValues = TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0001"));
      var withNulls = TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0002"));
      ArrangeDbContext.AddRange(withValues, withNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var loaded = await AssertDbContext.TestEntitiesWithStructsInOptionalComplexType.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls]);

      var structColumns = await TestEntityWithStructsInOptionalComplexType.QueryStructColumns(AssertDbContext, "\"TestEntitiesWithStructsInOptionalComplexType\"").ToListAsync();
      structColumns.Should().BeEquivalentTo(new[] { withValues, withNulls }.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
   }

   [Fact]
   public async Task Should_insert_null_for_struct_members_of_null_ComplexType()
   {
      var withValues = TestEntityWithStructsInOptionalComplexType.CreateWithValues(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0011"));
      var withNulls = TestEntityWithStructsInOptionalComplexType.CreateWithNulls(new Guid("9C1E2D3F-4A5B-4C6D-8E9F-0A1B2C3D0012"));

      await SUT.BulkInsertAsync([withValues, withNulls], new SqliteBulkInsertOptions());

      var loaded = await AssertDbContext.TestEntitiesWithStructsInOptionalComplexType.ToListAsync();
      loaded.Should().BeEquivalentTo([withValues, withNulls]);

      var structColumns = await TestEntityWithStructsInOptionalComplexType.QueryStructColumns(AssertDbContext, "\"TestEntitiesWithStructsInOptionalComplexType\"").ToListAsync();
      structColumns.Should().BeEquivalentTo(new[] { withValues, withNulls }.Select(TestEntityWithStructsInOptionalComplexType.StructColumns.From));
   }
}
