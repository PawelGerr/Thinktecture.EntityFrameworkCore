using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertAsync : IntegrationTestsBase
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

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

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
      var testEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      testEntity.SetPrivateField(3);

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqliteBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.GetPrivateField().Should().Be(3);
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
      AssertDbContext.Entry(loadedEntity!).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity!).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
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
                       PropertiesToInsert = EntityPropertiesProvider.From<TestEntityWithSqlDefaultValues>(e => new
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
                       PropertiesToInsert = EntityPropertiesProvider.From<TestEntityWithDotnetDefaultValues>(e => new
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
      loadedEntity!.Id.Should().Be(expectedId);
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
                                   PropertiesToInsert = new EntityPropertiesProvider(TestEntity.GetRequiredProperties())
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
                        .Should().ThrowAsync<SqliteException>().WithMessage("SQLite Error 19: 'NOT NULL constraint failed: TestEntities_Own_Inline_1.InlineEntity_IntColumn'.");
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

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions()))
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

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

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
                          SeparateEntities = new List<OwnedEntity>
                                             {
                                                new()
                                                {
                                                   IntColumn = 42,
                                                   StringColumn = "value 1"
                                                }
                                             }
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities = new List<OwnedEntity>
                                             {
                                                new()
                                                {
                                                   IntColumn = 42,
                                                   StringColumn = "value 1"
                                                }
                                             }
                       };
      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

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

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
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
                                            SeparateEntities = new List<OwnedEntity>
                                                               {
                                                                  new()
                                                                  {
                                                                     IntColumn = 43,
                                                                     StringColumn = "value 2"
                                                                  },
                                                                  new()
                                                                  {
                                                                     IntColumn = 44,
                                                                     StringColumn = "value 3"
                                                                  }
                                                               }
                                         }
                       };
      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
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

      await SUT.BulkInsertAsync(new[]
                                {
                                   testEntity
                                }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany_Inline()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_Inline
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities = new List<OwnedEntity_Owns_Inline>
                                             {
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
                                                }
                                             }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateMany()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateMany
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities = new List<OwnedEntity_Owns_SeparateMany>
                                             {
                                                new()
                                                {
                                                   IntColumn = 42,
                                                   StringColumn = "value 1",
                                                   SeparateEntities = new List<OwnedEntity>
                                                                      {
                                                                         new()
                                                                         {
                                                                            IntColumn = 43,
                                                                            StringColumn = "value 2"
                                                                         },
                                                                         new()
                                                                         {
                                                                            IntColumn = 44,
                                                                            StringColumn = "value 3"
                                                                         }
                                                                      }
                                                },
                                                new()
                                                {
                                                   IntColumn = 45,
                                                   StringColumn = "value 4",
                                                   SeparateEntities = new List<OwnedEntity>
                                                                      {
                                                                         new()
                                                                         {
                                                                            IntColumn = 46,
                                                                            StringColumn = "value 5"
                                                                         },
                                                                         new()
                                                                         {
                                                                            IntColumn = 47,
                                                                            StringColumn = "value 6"
                                                                         }
                                                                      }
                                                }
                                             }
                       };

      ActDbContext.Add(testEntity);

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities#OwnedEntity_Owns_SeparateMany.SeparateEntities' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities' is not supported.");
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateOne()
   {
      var testEntity = new TestEntity_Owns_SeparateMany_SeparateOne
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          SeparateEntities = new List<OwnedEntity_Owns_SeparateOne>
                                             {
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
                                                }
                                             }
                       };

      ActDbContext.Add(testEntity);

      await SUT.Awaiting(sut => sut.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions()))
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

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
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
                                              SeparateEntities = new List<OwnedEntity>
                                                                 {
                                                                    new()
                                                                    {
                                                                       IntColumn = 43,
                                                                       StringColumn = "value 2"
                                                                    },
                                                                    new()
                                                                    {
                                                                       IntColumn = 44,
                                                                       StringColumn = "value 3"
                                                                    }
                                                                 }
                                           }
                       };

      ActDbContext.Add(testEntity);

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
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

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqliteBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
   }
}
