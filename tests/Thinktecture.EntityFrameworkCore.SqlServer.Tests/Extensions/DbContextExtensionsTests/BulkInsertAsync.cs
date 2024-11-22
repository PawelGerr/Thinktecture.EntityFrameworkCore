using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertAsync : IntegrationTestsBase
{
   /// <inheritdoc />
   public BulkInsertAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
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

      await ActDbContext.BulkInsertAsync(testEntities);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                              Name = "Name",
                                              RequiredName = "RequiredName",
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
                          RequiredName = "RequiredName",
                          Count = 42
                       };
      var testEntities = new[] { testEntity };

      await ActDbContext.BulkInsertAsync(testEntities, new SqlServerBulkInsertOptions
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
                                              Count = 42
                                           });
   }

   [Fact]
   public async Task Should_insert_TestEntity_with_ComplexType()
   {
      var testEntity = new TestEntityWithComplexType(new Guid("54FF93FC-6BE9-4F19-A52E-E517CA9FEAA7"),
                                                     new BoundaryValueObject(2, 5));

      await ActDbContext.BulkInsertAsync(new[] { testEntity }, new SqlServerBulkInsertOptions());

      var loadedEntities = await AssertDbContext.TestEntities_with_ComplexType.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { testEntity });
   }
}
