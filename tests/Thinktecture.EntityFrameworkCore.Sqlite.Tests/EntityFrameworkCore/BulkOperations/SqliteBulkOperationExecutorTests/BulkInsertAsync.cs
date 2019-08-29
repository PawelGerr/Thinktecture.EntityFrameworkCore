using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertAsync : IntegrationTestsBase
   {
      private readonly SqliteBulkOperationExecutor _sut;

      public BulkInsertAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         var sqlGenerationHelperMock = new Mock<ISqlGenerationHelper>();
         sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>(), It.IsAny<string>()))
                                .Returns<string, string>((name, schema) => schema == null ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"");
         sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>())).Returns<string>(name => $"\"{name}\"");

         var logger = CreateDiagnosticsLogger<SqliteDbLoggerCategory.BulkOperation>();
         _sut = new SqliteBulkOperationExecutor(sqlGenerationHelperMock.Object, logger);
      }

      [Fact]
      public void Should_throw_when_inserting_queryType_without_providing_tablename()
      {
         ConfigureModel = builder => builder.Query<CustomTempTable>();

         _sut.Awaiting(sut => sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<CustomTempTable>(), new List<CustomTempTable>(), new SqliteBulkInsertOptions()))
             .Should().Throw<InvalidOperationException>();
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

         await _sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TestEntity>(), testEntities, new SqliteBulkInsertOptions());

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

         await _sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TestEntity>(), testEntities, new SqliteBulkInsertOptions());

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

         await _sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TestEntityWithShadowProperties>(), testEntities, new SqliteBulkInsertOptions());

         var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
         AssertDbContext.Entry(loadedEntity).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
         AssertDbContext.Entry(loadedEntity).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
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
         await _sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TestEntityWithAutoIncrement>(), testEntities, options);

         var loadedEntity = await AssertDbContext.TestEntitiesWithAutoIncrement.FirstOrDefaultAsync();
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
                             Count = 42,
                             PropertyWithBackingField = 7
                          };
         testEntity.SetPrivateField(3);
         var testEntities = new[] { testEntity };
         var idProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Id));
         var countProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Count));
         var propertyWithBackingField = typeof(TestEntity).GetProperty(nameof(TestEntity.PropertyWithBackingField));
         var privateField = typeof(TestEntity).GetField("_privateField", BindingFlags.Instance | BindingFlags.NonPublic);

         await _sut.BulkInsertAsync(ActDbContext,
                                    ActDbContext.GetEntityType<TestEntity>(),
                                    testEntities,
                                    new SqliteBulkInsertOptions
                                    {
                                       EntityMembersProvider = new EntityMembersProvider(new MemberInfo[]
                                                                                         {
                                                                                            idProperty,
                                                                                            countProperty,
                                                                                            propertyWithBackingField,
                                                                                            privateField
                                                                                         })
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
   }
}
