using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertAsync : IntegrationTestsBase
   {
      private SqlServerBulkOperationExecutor? _sut;

      private SqlServerBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqlServerBulkOperationExecutor>();

      public BulkInsertAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public void Should_throw_when_inserting_temp_table_entities_without_creating_table_first()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         SUT.Invoking(sut => sut.BulkInsertAsync(new List<TempTable<int>> { new(0) }, new SqlServerBulkInsertOptions()))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access destination table '[*].[#TempTable<int>]'.");
      }

      [Fact]
      public async Task Should_insert_column_with_converter()
      {
         var entities = new List<TestEntity> { new() { ConvertibleClass = new ConvertibleClass(42) } };

         await SUT.BulkInsertAsync(entities, new SqlServerBulkInsertOptions());

         var entity = AssertDbContext.TestEntities.Single();

         entity.ConvertibleClass.Should().NotBeNull();
         entity.ConvertibleClass!.Key.Should().Be(42);
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

         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

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
         var testEntity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
         testEntity.SetPrivateField(3);

         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

         var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
         loadedEntity.GetPrivateField().Should().Be(3);
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
         AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
         AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
      }

      [Fact]
      public void Should_throw_because_sqlbulkcopy_dont_support_null_for_NOT_NULL_despite_sql_default_value()
      {
         var testEntity = new TestEntityWithSqlDefaultValues { String = null! };
         var testEntities = new[] { testEntity };

         SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions()))
            .Should().Throw<InvalidOperationException>()
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
                                                 Id = Guid.Empty,     // persisted as-is
                                                 Int = 0,             // persisted as-is
                                                 NullableInt = 2,     // DEFAULT value constraint
                                                 String = "3",        // DEFAULT value constraint
                                                 NullableString = "4" // DEFAULT value constraint
                                              });
      }

      [Fact]
      public void Should_throw_because_sqlbulkcopy_dont_support_null_for_NOT_NULL_despite_dotnet_default_value()
      {
         var testEntity = new TestEntityWithDotnetDefaultValues { String = null! };
         var testEntities = new[] { testEntity };

         SUT.Awaiting(sut => sut.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions()))
            .Should().Throw<InvalidOperationException>()
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
         loadedEntity.Id.Should().Be(42);
      }

      [Fact]
      public async Task Should_ignore_auto_increment_column_without_KeepIdentity()
      {
         var testEntity = new TestEntityWithAutoIncrement { Id = 42, Name = "value" };
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

         var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
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
                             Count = 42,
                             PropertyWithBackingField = 7
                          };
         testEntity.SetPrivateField(3);
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities,
                                   new SqlServerBulkInsertOptions
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
      public void Should_throw_if_required_inlined_owned_type_is_null()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = null!
                          };
         var testEntities = new[] { testEntity };

         ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync(testEntities))
                     .Should().Throw<InvalidOperationException>().WithMessage("Column 'InlineEntity_IntColumn' does not allow DBNull.Value.");
      }

      [Fact]
      public async Task Should_insert_inlined_owned_type_if_it_has_default_values_only()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = new OwnedInlineEntity()
                          };
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

         var loadedEntities = await AssertDbContext.TestEntitiesOwningInlineEntity.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntityOwningInlineEntity
                                              {
                                                 Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                                 InlineEntity = new OwnedInlineEntity
                                                                {
                                                                   IntColumn = 0,
                                                                   StringColumn = null
                                                                }
                                              });
      }

      [Fact]
      public async Task Should_insert_inlined_owned_types()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = new OwnedInlineEntity
                                            {
                                               IntColumn = 42,
                                               StringColumn = "value"
                                            }
                          };
         var testEntities = new[] { testEntity };

         await SUT.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions());

         var loadedEntities = await AssertDbContext.TestEntitiesOwningInlineEntity.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntityOwningInlineEntity
                                              {
                                                 Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                                 InlineEntity = new OwnedInlineEntity
                                                                {
                                                                   IntColumn = 42,
                                                                   StringColumn = "value"
                                                                }
                                              });
      }
   }
}
