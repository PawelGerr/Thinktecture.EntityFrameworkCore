using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertAsync : IntegrationTestsBase
{
   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public BulkInsertAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Invoking(sut => sut.BulkInsertAsync(new List<TempTable<int>> { new(0) }, new NpgsqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_insert_entities()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };

      var testEntities = new[] { testEntity };

      var affectedRows = await SUT.BulkInsertAsync(testEntities, new NpgsqlBulkInsertOptions());

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
   public async Task Should_insert_column_with_converter()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var entities = new List<TestEntity> { new() { ConvertibleClass = new ConvertibleClass(42), RequiredName = "RequiredName" } };

      await SUT.BulkInsertAsync(entities, new NpgsqlBulkInsertOptions());

      var entity = AssertDbContext.TestEntities.Single();

      entity.ConvertibleClass.Should().NotBeNull();
      entity.ConvertibleClass!.Key.Should().Be(42);
   }

   [Fact]
   public async Task Should_insert_private_property()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var testEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" };
      testEntity.SetPrivateField(3);

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new NpgsqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity!.GetPrivateField().Should().Be(3);
   }

   [Fact]
   public async Task Should_insert_flags_enum()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithFlags>();

      var testEntity = new TestEntityWithFlags
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          GroupId = 1,
                          Phase = PhaseMembership.A | PhaseMembership.C
                       };

      await SUT.BulkInsertAsync(new[] { testEntity }, new NpgsqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithFlags.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.Phase.Should().Be(PhaseMembership.A | PhaseMembership.C);
   }

   [Fact]
   public async Task Should_insert_shadow_properties()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithShadowProperties>();

      var testEntity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ActDbContext.Entry(testEntity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(testEntity).Property("ShadowIntProperty").CurrentValue = 42;

      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new NpgsqlBulkInsertOptions());

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
   }

   [Fact]
   public async Task Should_insert_specified_properties_only()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity>();

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
                                new NpgsqlBulkInsertOptions
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
   public async Task Should_insert_auto_increment_column()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithAutoIncrement>();

      var testEntity = new TestEntityWithAutoIncrement { Name = "value" };
      var testEntities = new[] { testEntity };

      await SUT.BulkInsertAsync(testEntities, new NpgsqlBulkInsertOptions
                                               {
                                                  PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntityWithAutoIncrement>(e => e.Name)
                                               });

      var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity.Id.Should().NotBe(0);
      loadedEntity.Name.Should().Be("value");
   }

   [Fact]
   public async Task Should_insert_inlined_owned_types()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_Inline>();

      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = new OwnedEntity
                                         {
                                            IntColumn = 42,
                                            StringColumn = "value"
                                         }
                       };

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

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
   public async Task Should_insert_TestEntity_Owns_SeparateOne()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateOne>(true);

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

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateMany>(true);

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

      var affectedRows = await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      affectedRows.Should().Be(3);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(testEntity);
   }

   [Fact]
   public async Task Should_insert_inlined_owned_type_if_it_has_default_values_only()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_Inline>();

      var testEntity = new TestEntity_Owns_Inline
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          InlineEntity = new OwnedEntity()
                       };

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

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
   public async Task Should_throw_if_separated_owned_type_uses_shadow_property_id_and_is_detached()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateOne>(true);

      var testEntity = new TestEntity_Owns_SeparateOne
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          SeparateEntity = new OwnedEntity
                                           {
                                              IntColumn = 42,
                                              StringColumn = "value"
                                           }
                       };

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");
   }

   [Fact]
   public async Task Should_throw_if_separated_owned_types_uses_shadow_property_id_and_is_detached()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateMany>(true);

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("The entity type 'OwnedEntity' uses a shared type and the supplied entity is currently not being tracked. To start tracking this entity, call '.Reference().TargetEntry' or '.Collection().FindEntry()' on the owner entry.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_Inline()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_Inline_Inline>();

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateMany()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_Inline_SeparateMany>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_Inline_SeparateOne()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_Inline_SeparateOne>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateMany_Inline()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateMany_Inline>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateMany_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateMany()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateMany_SeparateMany>(true);

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities#OwnedEntity_Owns_SeparateMany.SeparateEntities' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateMany.SeparateEntities' is not supported.");
   }

   [Fact]
   public async Task Should_throw_on_insert_of_TestEntity_Owns_SeparateMany_SeparateOne()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateMany_SeparateOne>(true);

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

      await SUT.Awaiting(sut => sut.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions()))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Non-inlined (i.e. with its own table) nested owned type 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities#OwnedEntity_Owns_SeparateOne.SeparateEntity' inside another owned type collection 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateMany_SeparateOne.SeparateEntities' is not supported.");
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_Inline()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateOne_Inline>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateMany()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateOne_SeparateMany>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateMany.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_Owns_SeparateOne_SeparateOne()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntity_Owns_SeparateOne_SeparateOne>(true);

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

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithComplexType>();

      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_entity_with_struct_ComplexType()
   {
      var testEntity = new TestEntityWithStructComplexType
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          MessageTimestamp = 10,
                          EffectiveMessage = new TimestampPair(100, 200),
                          Name = "Name"
                       };

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntitiesWithStructComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);

      // The COPY command is not a DbCommand, so the configured column names are verified by reading them directly.
      var columnSum = await AssertDbContext.Database
                                          .SqlQueryRaw<long>($"""SELECT "effective_received_at" + "effective_message_timestamp" AS "Value" FROM "{Schema}"."TestEntitiesWithStructComplexType" """)
                                          .SingleAsync();
      columnSum.Should().Be(300);
   }

   [Fact]
   public async Task Should_insert_entity_with_value_converted_members_of_struct_ComplexType()
   {
      var testEntity = new TestEntityWithConvertedComplexType
                       {
                          Id = new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                          MessageTimestamp = 10,
                          EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200))
                       };

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntitiesWithConvertedComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([testEntity]);
   }

   [Fact]
   public async Task Should_insert_entities_with_freeze()
   {
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var testEntity = new TestEntity
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };

      await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { Freeze = true });
      await transaction.CommitAsync();

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
   public async Task Should_insert_multiple_entities_with_freeze()
   {
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();
      await SUT.TruncateTableAsync<TestEntity>();

      var entity1 = new TestEntity
                    {
                       Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                       Name = "Name1",
                       RequiredName = "RequiredName1",
                       Count = 1
                    };
      var entity2 = new TestEntity
                    {
                       Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                       Name = "Name2",
                       RequiredName = "RequiredName2",
                       Count = 2
                    };

      await SUT.BulkInsertAsync([entity1, entity2], new NpgsqlBulkInsertOptions { Freeze = true });
      await transaction.CommitAsync();

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo([entity1, entity2]);
   }

   [Fact]
   public async Task Should_insert_into_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_BulkInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL)
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

         await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableName = "TestEntities_BulkInsertRedirect", Schema = Schema });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_BulkInsertRedirect" """)
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
         redirectedIds[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_BulkInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntity_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_BulkInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL)
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

         await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_BulkInsertRedirect", Schema) });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_BulkInsertRedirect" """)
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
         redirectedIds[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_BulkInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntityWithDefaultSchema_provider()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_BulkInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL)
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

         await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("TestEntities_BulkInsertRedirect") });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_BulkInsertRedirect" """)
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
         redirectedIds[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_BulkInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_into_table_from_ForRootEntityWithDefaultName_provider()
   {
      var redirectSchema = $"{Schema}_BulkInsertRedirect";

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE SCHEMA "{redirectSchema}";
         CREATE TABLE "{redirectSchema}"."TestEntities" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
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

         await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(redirectSchema) });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(0, "original table should be empty");

         var redirectedIds = await AssertDbContext.Database
                                                 .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{redirectSchema}"."TestEntities" """)
                                                 .ToListAsync();
         redirectedIds.Should().HaveCount(1);
         redirectedIds[0].Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP SCHEMA IF EXISTS "{redirectSchema}" CASCADE""");
      }
   }

   // https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/issues/76
   // The owned entity must not be written to the table of the model. The bulk insert fails when it reaches the owned type,
   // and the owner row stays because the library adds no transaction.
   [Fact]
   public async Task Should_not_insert_separated_owned_entities_into_model_table_when_table_name_is_overridden()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_Own_SeparateOne_BulkInsertRedirect" (LIKE "{Schema}"."TestEntities_Own_SeparateOne" INCLUDING ALL)
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
            await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne_BulkInsertRedirect" });
         }
         catch (Exception ex)
         {
            exception = ex;
         }

         var ownedEntityIdsInModelTable = await AssertDbContext.Database
                                                               .SqlQueryRaw<Guid>($"""SELECT "TestEntity_Owns_SeparateOneId" FROM "{Schema}"."SeparateEntitiesOne" """)
                                                               .ToListAsync();
         ownedEntityIdsInModelTable.Should().BeEmpty("the owned entity must not be written to the table of the model when the owner is redirected");

         exception.Should().BeOfType<InvalidOperationException>()
                  .Which.Message.Should().Contain("override the table of the root entity only");

         var ownerIds = await AssertDbContext.Database
                                             .SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateOne_BulkInsertRedirect" """)
                                             .ToListAsync();
         ownerIds.Should().BeEquivalentTo([testEntity.Id]);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_Own_SeparateOne_BulkInsertRedirect" """);
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

      await SUT.Awaiting(s => s.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableName = "TestEntities_Own_SeparateOne" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");

      // The library adds no transaction, so the root row stays when the operation reaches the owned type.
      (await AssertDbContext.Database.SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateOne" """).ToListAsync()).Should().BeEquivalentTo([testEntity.Id]);
      (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();
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

      var options = new NpgsqlBulkInsertOptions
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
                                         : new NpgsqlBulkInsertOptions { TableNameProvider = provider };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(2);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();

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

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableNameProvider = provider });

         affectedRows.Should().Be(3);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateOne_SeparateOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne_SeparateOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([42]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne_SeparateOne_Inner_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([43]);
         (await AssertDbContext.TestEntities_Own_SeparateOne_SeparateOne.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne_SeparateOne" """).ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne_SeparateOne_Inner" """).ToListAsync()).Should().BeEmpty();

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

         var affectedRows = await SUT.BulkInsertAsync([testEntity], new NpgsqlBulkInsertOptions { TableNameProvider = new SuffixTableNameProvider("_Redirect") });

         affectedRows.Should().Be(3);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateMany_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesMany_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([1, 2]);
         (await AssertDbContext.TestEntities_Own_SeparateMany.ToListAsync()).Should().BeEmpty();
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesMany" """).ToListAsync()).Should().BeEmpty();
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

         var options = new NpgsqlBulkInsertOptions
                       {
                          TableName = "TestEntities_Own_SeparateOne_Redirect",
                          Schema = Schema,
                          PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => e.Id)
                       };

         var affectedRows = await SUT.BulkInsertAsync([testEntity], options);

         affectedRows.Should().Be(1);
         (await AssertDbContext.Database.SqlQueryRaw<Guid>($"""SELECT "Id" FROM "{Schema}"."TestEntities_Own_SeparateOne_Redirect" """).ToListAsync())
            .Should().BeEquivalentTo([testEntity.Id]);
         (await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "IntColumn" FROM "{Schema}"."SeparateEntitiesOne" """).ToListAsync()).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync("TestEntities_Own_SeparateOne_Redirect");
      }
   }

   // LIKE copies the columns and the primary key, but no foreign keys.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""CREATE TABLE "{Schema}"."{targetTableName}" (LIKE "{Schema}"."{sourceTableName}" INCLUDING ALL)""");
   }

   private async Task DropTablesAsync(params string[] tableNames)
   {
      foreach (var tableName in tableNames)
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."{tableName}" """);
      }
   }

   [Fact]
   public async Task Should_insert_entity_with_jsonb_column()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithJsonColumns>();

      var entity = new TestEntityWithJsonColumns
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      JsonbColumn = """{"key": "value"}""",
                      JsonColumn = null
                   };

      var affectedRows = await SUT.BulkInsertAsync([entity], new NpgsqlBulkInsertOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntity.JsonbColumn.Should().Be("""{"key": "value"}""");
      loadedEntity.JsonColumn.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_entity_with_json_column()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithJsonColumns>();

      var entity = new TestEntityWithJsonColumns
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      JsonbColumn = null,
                      JsonColumn = """{"key": "value"}"""
                   };

      var affectedRows = await SUT.BulkInsertAsync([entity], new NpgsqlBulkInsertOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntity.JsonbColumn.Should().BeNull();
      loadedEntity.JsonColumn.Should().Be("""{"key": "value"}""");
   }

   [Fact]
   public async Task Should_insert_entity_with_null_jsonb_column()
   {
      await ArrangeDbContext.Database.EnsureCreatedAsync();
      await SUT.TruncateTableAsync<TestEntityWithJsonColumns>();

      var entity = new TestEntityWithJsonColumns
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      JsonbColumn = null,
                      JsonColumn = null
                   };

      var affectedRows = await SUT.BulkInsertAsync([entity], new NpgsqlBulkInsertOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithJsonColumns.SingleAsync();
      loadedEntity.Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntity.JsonbColumn.Should().BeNull();
      loadedEntity.JsonColumn.Should().BeNull();
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

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new NpgsqlBulkInsertOptions { TableName = "TestEntities", Schema = Schema });

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

      var affectedRows = await SUT.BulkInsertAsync(new[] { entity }, new NpgsqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities", Schema) });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[0].Name.Should().Be("Name");
      loadedEntities[0].RequiredName.Should().Be("RequiredName");
      loadedEntities[0].Count.Should().Be(42);
   }
}
