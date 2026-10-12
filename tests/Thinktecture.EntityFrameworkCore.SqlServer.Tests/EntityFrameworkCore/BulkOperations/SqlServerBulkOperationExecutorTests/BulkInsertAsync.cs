using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertAsync : IntegrationTestsBase
{
   private SqlServerBulkOperationExecutor? _sut;

   private SqlServerBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqlServerBulkOperationExecutor>();

   public BulkInsertAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Invoking(sut => sut.BulkInsertAsync(new List<TempTable<int>> { new(0) }, new SqlServerBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_insert_column_with_converter()
   {
      var entities = new List<TestEntity> { new() { ConvertibleClass = new ConvertibleClass(42), RequiredName = "RequiredName" } };

      await SUT.BulkInsertAsync(entities, new SqlServerBulkInsertOptions());

      var entity = AssertDbContext.TestEntities.Single();

      entity.ConvertibleClass.Should().NotBeNull();
      entity.ConvertibleClass!.Key.Should().Be(42);
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

      await SUT.BulkInsertAsync(new[] { testEntity }, new SqlServerBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
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

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

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
   public async Task Should_insert_private_property()
   {
      var testEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      testEntity.SetPrivateField(3);

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity!.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      var testEntity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ActDbContext.Entry(testEntity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(testEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
   }

   [Fact]
   public async Task Should_throw_because_sqlbulkcopy_dont_support_null_for_NOT_NULL_despite_sql_default_value()
   {
      var testEntity = new TestEntityWithSqlDefaultValues { String = null! };
      var testEntities = new[] { testEntity };

      await SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("Column 'String' does not allow DBNull.Value.");
   }

   [Fact]
   public async Task Should_write_not_nullable_structs_as_is_despite_sql_default_value()
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

      var options = new SqlServerBulkInsertOptions
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
                                              Id = Guid.Empty,     // persisted as-is
                                              Int = 0,             // persisted as-is
                                              NullableInt = 2,     // DEFAULT value constraint
                                              String = "3",        // DEFAULT value constraint
                                              NullableString = "4" // DEFAULT value constraint
                                           });
   }

   [Fact]
   public async Task Should_throw_because_sqlbulkcopy_dont_support_null_for_NOT_NULL_despite_dotnet_default_value()
   {
      var testEntity = new TestEntityWithDotnetDefaultValues { String = null! };
      var testEntities = new[] { testEntity };

      await SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("Column 'String' does not allow DBNull.Value.");
   }

   [Fact]
   public async Task Should_write_not_nullable_structs_as_is_despite_dotnet_default_value()
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

      var options = new SqlServerBulkInsertOptions
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
      loadedEntity.Should().BeEquivalentTo(new TestEntityWithDotnetDefaultValues
                                           {
                                              Id = Guid.Empty,     // persisted as-is
                                              Int = 0,             // persisted as-is
                                              NullableInt = 2,     // DEFAULT value constraint
                                              String = "3",        // DEFAULT value constraint
                                              NullableString = "4" // DEFAULT value constraint
                                           });
   }

   [Fact]
   public async Task Should_insert_auto_increment_column_with_KeepIdentity()
   {
      var testEntity = new TestEntityWithAutoIncrement { Id = 42 };
      var testEntities = new[] { testEntity };

      var options = new SqlServerBulkInsertOptions { SqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity };
      await SUT.BulkInsertAsync(testEntities, options);

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.Id.Should().Be(42);
   }

   [Fact]
   public async Task Should_ignore_auto_increment_column_without_KeepIdentity()
   {
      var testEntity = new TestEntityWithAutoIncrement { Id = 42, Name = "value" };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.Id.Should().NotBe(0);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_ignore_RowVersion()
   {
      var testEntity = new TestEntityWithRowVersion { Id = new Guid("EBC95620-4D80-4318-9B92-AD7528B2965C"), RowVersion = Int32.MaxValue };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithRowVersion.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.Id.Should().Be(new Guid("EBC95620-4D80-4318-9B92-AD7528B2965C"));
      loadedEntity.RowVersion.Should().NotBe(Int32.MaxValue);
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

      await SUT.BulkInsertAsync([testEntity],
                                new SqlServerBulkInsertOptions
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
   public async Task Should_throw_entity_contains_inlined_owned_type()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = null!
                       };

      await ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync([testEntity]))
                        .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_insert_inlined_owned_type_if_it_has_default_values_only()
   {
      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = new OwnedEntity()
                       };

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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
   public async Task Should_insert_inlined_owned_types()
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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions()))
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

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions()))
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

                             new()
                             {
                                IntColumn = 43,
                                StringColumn = "value 2"
                             },
                          ]
                       };
      ActDbContext.Add(testEntity);

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

      affectedRows.Should().Be(3);

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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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
      ], new SqlServerBulkInsertOptions());

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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions()))
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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions()))
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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

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

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_into_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         SELECT TOP 0 * INTO [{Schema}].[TestEntities_BulkInsertRedirect] FROM [{Schema}].[TestEntities];
         ALTER TABLE [{Schema}].[TestEntities_BulkInsertRedirect] ADD PRIMARY KEY ([Id]);
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

         await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions { TableName = "TestEntities_BulkInsertRedirect" });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedEntities = await AssertDbContext.Database
                                                      .SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_BulkInsertRedirect]")
                                                      .ToListAsync();
         redirectedEntities.Should().HaveCount(1);
         redirectedEntities[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS [{Schema}].[TestEntities_BulkInsertRedirect]");
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         SELECT TOP 0 * INTO [{Schema}].[TestEntities_BulkInsertRedirect] FROM [{Schema}].[TestEntities];
         ALTER TABLE [{Schema}].[TestEntities_BulkInsertRedirect] ADD PRIMARY KEY ([Id]);
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

         await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect", Schema) });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedEntities = await AssertDbContext.Database
                                                      .SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_BulkInsertRedirect]")
                                                      .ToListAsync();
         redirectedEntities.Should().HaveCount(1);
         redirectedEntities[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS [{Schema}].[TestEntities_BulkInsertRedirect]");
      }
   }

   // https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/issues/76
   [Fact]
   public async Task Should_not_insert_separated_owned_entities_into_model_table_when_table_name_is_overridden()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         SELECT TOP 0 * INTO [{Schema}].[TestEntities_Own_SeparateOne_BulkInsertRedirect] FROM [{Schema}].[TestEntities_Own_SeparateOne];
         ALTER TABLE [{Schema}].[TestEntities_Own_SeparateOne_BulkInsertRedirect] ADD PRIMARY KEY ([Id]);
         """);

      try
      {
         var options = new SqlServerBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne_BulkInsertRedirect" };

         await BulkInsertSeparateOneAndAssertNoStrayRowsAsync(options, Schema!, "TestEntities_Own_SeparateOne_BulkInsertRedirect");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS [{Schema}].[TestEntities_Own_SeparateOne_BulkInsertRedirect]");
      }
   }

   // https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/issues/76
   [Fact]
   public async Task Should_not_insert_separated_owned_entities_into_model_schema_when_schema_is_overridden()
   {
      var redirectSchema = $"{Schema}_BulkInsertRedirect";

      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA [{redirectSchema}]");

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
            SELECT TOP 0 * INTO [{redirectSchema}].[TestEntities_Own_SeparateOne] FROM [{Schema}].[TestEntities_Own_SeparateOne];
            ALTER TABLE [{redirectSchema}].[TestEntities_Own_SeparateOne] ADD PRIMARY KEY ([Id]);
            """);

         var options = new SqlServerBulkInsertOptions { Schema = redirectSchema };

         await BulkInsertSeparateOneAndAssertNoStrayRowsAsync(options, redirectSchema, "TestEntities_Own_SeparateOne");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""
            DROP TABLE IF EXISTS [{redirectSchema}].[TestEntities_Own_SeparateOne];
            DROP SCHEMA IF EXISTS [{redirectSchema}];
            """);
      }
   }

   // The owned entity must not be written to the table of the model. The bulk insert fails when it reaches the owned type,
   // and the owner row stays because the library adds no transaction.
   private async Task BulkInsertSeparateOneAndAssertNoStrayRowsAsync(
      SqlServerBulkInsertOptions options,
      string redirectedSchema,
      string redirectedTableName)
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
         await SUT.BulkInsertAsync([testEntity], options);
      }
      catch (Exception ex)
      {
         exception = ex;
      }

      var ownedEntityIdsInModelTable = await AssertDbContext.Database
                                                            .SqlQueryRaw<Guid>($"SELECT [TestEntity_Owns_SeparateOneId] FROM [{Schema}].[SeparateEntitiesOne]")
                                                            .ToListAsync();
      ownedEntityIdsInModelTable.Should().BeEmpty("the owned entity must not be written to the table of the model when the owner is redirected");

      exception.Should().BeOfType<InvalidOperationException>()
               .Which.Message.Should().Contain("override the table of the root entity only");

      var ownerIds = await AssertDbContext.Database
                                          .SqlQueryRaw<Guid>($"SELECT [Id] FROM [{redirectedSchema}].[{redirectedTableName}]")
                                          .ToListAsync();
      ownerIds.Should().BeEquivalentTo([testEntity.Id]);
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

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");

      // The library adds no transaction, so the root row stays when the operation reaches the owned type.
      (await AssertDbContext.Database.SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_Own_SeparateOne]").ToListAsync()).Should().BeEquivalentTo([testEntity.Id]);
      (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne]").ToListAsync()).Should().BeEmpty();
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

      var options = new SqlServerBulkInsertOptions
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
                             Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                             SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                          };
         ActDbContext.Add(testEntity);
         var provider = new SuffixTableNameProvider("_Redirect");
         IBulkInsertOptions options = useGenericOptions
                                         ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                         : new SqlServerBulkInsertOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(2);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_Own_SeparateOne_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne]").ToListAsync()).Should().BeEmpty();

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

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions { TableNameProvider = provider });

         affectedRows.Should().Be(3);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_Own_SeparateOne_SeparateOne_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne_SeparateOne_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne_SeparateOne_Inner_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([43]);
         (await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne_SeparateOne]").ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne_SeparateOne_Inner]").ToListAsync()).Should().BeEmpty();

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
                             SeparateEntities =
                             [
                                new() { IntColumn = 1, StringColumn = "value 1" },
                                new() { IntColumn = 2, StringColumn = "value 2" }
                             ]
                          };
         ActDbContext.Add(testEntity);

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new SqlServerBulkInsertOptions { TableNameProvider = new SuffixTableNameProvider("_Redirect") });

         affectedRows.Should().Be(3);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_Own_SeparateMany_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesMany_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([1, 2]);
         (await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesMany]").ToListAsync()).Should().BeEmpty();
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

         var options = new SqlServerBulkInsertOptions
                       {
                          TableName = "TestEntities_Own_SeparateOne_Redirect",
                          PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => e.Id)
                       };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(1);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"SELECT [Id] FROM [{Schema}].[TestEntities_Own_SeparateOne_Redirect]").ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"SELECT [IntColumn] FROM [{Schema}].[SeparateEntitiesOne]").ToListAsync()).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_Redirect");
      }
   }

   // Copies the columns of the model table without foreign keys. The primary key is added for the root tables only.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"SELECT TOP 0 * INTO [{Schema}].[{targetTableName}] FROM [{Schema}].[{sourceTableName}];");

      if (sourceTableName.StartsWith("TestEntities_", StringComparison.Ordinal))
         await ActDbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE [{Schema}].[{targetTableName}] ADD PRIMARY KEY ([Id]);");
   }

   private async Task DropTablesAsync(params string[] tableNames)
   {
      foreach (var tableName in tableNames)
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS [{Schema}].[{tableName}]");
      }
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

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new SqlServerBulkInsertOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
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
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new SqlServerBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities", Schema) });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[0].Name.Should().Be("Name");
      loadedEntities[0].RequiredName.Should().Be("RequiredName");
      loadedEntities[0].Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_throw_when_type_is_not_in_model()
   {
      await SUT.Invoking(sut => sut.BulkInsertAsync(new List<TestEntityTempTable> { new() }, new SqlServerBulkInsertOptions()))
               .Should().ThrowAsync<EntityTypeNotFoundException>();
   }
}
