using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertAsync : IntegrationTestsBase
   {
      private readonly SqlServerBulkOperationExecutor _sut = new SqlServerBulkOperationExecutor();

      public BulkInsertAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public void Should_throw_when_inserting_queryType_without_providing_tablename()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         _sut.Awaiting(sut => sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int>>(), new List<TempTable<int>>(), new SqlBulkInsertOptions()))
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
         testEntity.SetPrivateField(3);

         var testEntities = new[] { testEntity };

         await _sut.BulkInsertAsync(ActDbContext, ActDbContext.GetEntityType<TestEntity>(), testEntities, new SqlBulkInsertOptions());

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntity
                                              {
                                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                 Name = "Name",
                                                 Count = 42
                                              });
         loadedEntity.GetPrivateField().Should().Be(3);
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
                                    new SqlBulkInsertOptions
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
