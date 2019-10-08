using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertAsync : IntegrationTestsBase
   {
      /// <inheritdoc />
      public BulkInsertAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
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

         await ActDbContext.BulkInsertAsync(testEntities);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntity
                                              {
                                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                 Name = "Name",
                                                 Count = 42
                                              });
      }

      [Fact]
      public async Task Should_insert_specified_properties_only()
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             Count = 42
                          };
         var testEntities = new[] { testEntity };

         var idProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Id)) ?? throw new Exception($"Property {nameof(TestEntity.Id)} not found.");
         var countProperty = typeof(TestEntity).GetProperty(nameof(TestEntity.Count)) ?? throw new Exception($"Property {nameof(TestEntity.Count)} not found.");
         var propertyWithBackingField = typeof(TestEntity).GetProperty(nameof(TestEntity.PropertyWithBackingField)) ?? throw new Exception($"Property {nameof(TestEntity.PropertyWithBackingField)} not found.");
         var privateField = typeof(TestEntity).GetField("_privateField", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception($"Field _privateField not found.");

         await ActDbContext.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions
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
                                                 Count = 42
                                              });
      }
   }
}
