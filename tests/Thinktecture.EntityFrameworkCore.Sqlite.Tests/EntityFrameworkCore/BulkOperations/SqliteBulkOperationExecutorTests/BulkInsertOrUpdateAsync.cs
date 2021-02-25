using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertOrUpdateAsync : IntegrationTestsBase
   {
      private SqliteBulkOperationExecutor? _sut;

      private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

      public BulkInsertOrUpdateAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_throw_when_entity_has_no_key()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         SUT.Invoking(sut => sut.BulkInsertOrUpdateAsync(new List<TempTable<int>> { new(0) }, new SqliteBulkInsertOrUpdateOptions()))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The entity 'Thinktecture.EntityFrameworkCore.TempTables.TempTable<int>' has no primary key. Please provide key properties to perform JOIN/match on.");
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
         var existingEntity = new TestEntity { Id = new Guid("79DA4171-C90B-4A5D-B0B5-D0A1E1BDF966") };
         ArrangeDbContext.Add(existingEntity);
         await ArrangeDbContext.SaveChangesAsync();

         existingEntity.ConvertibleClass = new ConvertibleClass(43);
         var newEntity = new TestEntity
                         {
                            Id = new Guid("3DAEA618-B732-4BCA-A5A1-D1E075022DEC"),
                            ConvertibleClass = new ConvertibleClass(42)
                         };

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { existingEntity, newEntity }, new SqliteBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);

         var entities = AssertDbContext.TestEntities.ToList();
         entities.Should().HaveCount(2)
                 .And.Subject.Should().BeEquivalentTo(new TestEntity { Id = new Guid("79DA4171-C90B-4A5D-B0B5-D0A1E1BDF966"), ConvertibleClass = new ConvertibleClass(43) },
                                                      new TestEntity { Id = new Guid("3DAEA618-B732-4BCA-A5A1-D1E075022DEC"), ConvertibleClass = new ConvertibleClass(42) });
      }

      [Fact]
      public async Task Should_insert_entities()
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
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
                                                   Count = 42
                                                });
      }

      [Fact]
      public async Task Should_insert_private_property()
      {
         var existingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC") };
         ArrangeDbContext.Add(existingEntity);
         await ArrangeDbContext.SaveChangesAsync();

         existingEntity.SetPrivateField(1);

         var newEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         newEntity.SetPrivateField(3);

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { newEntity, existingEntity }, new SqliteBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);

         var expectedExistingEntity = new TestEntity { Id = new Guid("7C200656-E633-4F93-9F73-C5C7628196DC") };
         expectedExistingEntity.SetPrivateField(1);

         var expectedNewEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         expectedNewEntity.SetPrivateField(3);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(expectedNewEntity, expectedExistingEntity);
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
      public async Task Should_insert_entity_with_auto_increment_column()
      {
         var testEntity = new TestEntityWithAutoIncrement { Name = "Name" };
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertOrUpdateAsync(testEntities, new SqliteBulkInsertOrUpdateOptions
                                                         {
                                                            PropertiesToInsert = EntityPropertiesProvider.From<TestEntityWithAutoIncrement>(entity => entity.Name)
                                                         });

         var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
         loadedEntity.Id.Should().NotBe(0);
      }

      [Fact]
      public async Task Should_insert_specified_properties_only()
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             Count = 42,
                             PropertyWithBackingField = 7
                          };
         testEntity.SetPrivateField(3);
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertOrUpdateAsync(testEntities,
                                           new SqliteBulkInsertOrUpdateOptions
                                           {
                                              PropertiesToInsert = new EntityPropertiesProvider(TestEntity.GetRequiredProperties())
                                           });

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntity
                                              {
                                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                 Count = 42,
                                                 PropertyWithBackingField = 7
                                              });
         loadedEntity.GetPrivateField().Should().Be(3);
      }

      [Fact]
      public async Task Should_not_throw_if_key_property_is_not_present_in_PropertiesToUpdate()
      {
         var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => new { entity.Name });

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new List<TestEntity>(), new SqliteBulkInsertOrUpdateOptions
                                                                                      {
                                                                                         PropertiesToUpdate = propertiesProvider
                                                                                      });

         affectedRows.Should().Be(0);
      }

      [Fact]
      public async Task Should_update_entities()
      {
         var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE") };
         ArrangeDbContext.AddRange(entity_1, entity_2);
         await ArrangeDbContext.SaveChangesAsync();

         entity_1.Name = "Name";
         entity_1.Count = 1;
         entity_2.Name = "OtherName";
         entity_2.Count = 2;

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkInsertOrUpdateOptions());

         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(entity_1, entity_2);
      }

      [Fact]
      public void Should_update_entities_based_on_non_pk_property()
      {
         var keyProperties = EntityPropertiesProvider.From<TestEntity>(e => e.Name);

         SUT.Awaiting(sut => sut.BulkInsertOrUpdateAsync(new TestEntity[0], new SqliteBulkInsertOrUpdateOptions { KeyProperties = keyProperties }))
            .Should().Throw<InvalidOperationException>().WithMessage("Error during bulk operation on table '\"TestEntities\"'. See inner exception for more details.")
            .WithInnerException<SqliteException>().WithMessage("SQLite Error 1: 'ON CONFLICT clause does not match any PRIMARY KEY or UNIQUE constraint'.");
      }

      [Fact]
      public async Task Should_update_provided_entity_only()
      {
         var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE") };
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
         loadedEntities.Should().HaveCount(2)
                       .And.Subject
                       .Should().BeEquivalentTo(entity_1, entity_2);
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
         var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "original value" };
         ArrangeDbContext.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         entity.Name = "Name"; // this would not be updated
         entity.Count = 42;
         entity.PropertyWithBackingField = 7;
         entity.SetPrivateField(3);

         var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { entity },
                                                              new SqliteBulkInsertOrUpdateOptions
                                                              {
                                                                 PropertiesToUpdate = new EntityPropertiesProvider(TestEntity.GetRequiredProperties())
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
                                                 Name = "original value"
                                              });
         loadedEntity.GetPrivateField().Should().Be(3);
      }
   }
}
