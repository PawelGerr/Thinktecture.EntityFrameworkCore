using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateAsync : IntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;

   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_not_throw_if_entities_is_empty()
   {
      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity>(), new SqliteBulkUpdateOptions());
      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_not_throw_if_key_property_is_not_part_of_PropertiesToUpdate()
   {
      var propertiesProvider = EntityPropertiesProvider.From<TestEntity>(entity => new { entity.Name });

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity>(), new SqliteBulkUpdateOptions
                                                                           {
                                                                              PropertiesToUpdate = propertiesProvider
                                                                           });

      affectedRows.Should().Be(0);
   }

   [Fact]
   public async Task Should_update_column_with_converter()
   {
      var entity = new TestEntity();
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.ConvertibleClass = new ConvertibleClass(43);

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity> { entity }, new SqliteBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = AssertDbContext.TestEntities.Single();
      loadedEntity.ConvertibleClass.Should().NotBeNull();
      loadedEntity.ConvertibleClass!.Key.Should().Be(43);
   }

   [Fact]
   public async Task Should_return_0_if_no_rows_match()
   {
      var entity = new TestEntity { Id = new Guid("5B9587A3-2312-43DF-9681-38EC22AD8606") };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await SUT.BulkUpdateAsync(new List<TestEntity> { new() { Id = new Guid("506E664A-9ADC-4221-9577-71DCFD73DE64") } }, new SqliteBulkUpdateOptions());

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

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkUpdateOptions());

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { entity_1, entity_2 });
   }

   [Fact]
   public async Task Should_update_entities_based_on_non_pk_property()
   {
      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "value" };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = null };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      entity_1.Name = null;
      entity_1.Count = 1;
      entity_2.Name = "value";
      entity_2.Count = 2;

      var properties = EntityPropertiesProvider.From<TestEntity>(e => new { e.Name, e.Count });
      var keyProperties = EntityPropertiesProvider.From<TestEntity>(e => e.Name);
      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity_1, entity_2 }, new SqliteBulkUpdateOptions { PropertiesToUpdate = properties, KeyProperties = keyProperties });

      affectedRows.Should().Be(2);

      entity_1.Name = "value";
      entity_2.Name = null;

      entity_1.Count = 2;
      entity_2.Count = 1;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { entity_1, entity_2 });
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

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity_1 }, new SqliteBulkUpdateOptions());

      affectedRows.Should().Be(1);

      entity_2.Name = default;
      entity_2.Count = default;

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { entity_1, entity_2 });
   }

   [Fact]
   public async Task Should_update_private_property()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      entity.SetPrivateField(1);

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      loadedEntity!.GetPrivateField().Should().Be(1);
   }

   [Fact]
   public async Task Should_update_shadow_properties()
   {
      var entity = new TestEntityWithShadowProperties { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      ActDbContext.Entry(entity).Property("ShadowStringProperty").CurrentValue = "value";
      ActDbContext.Entry(entity).Property("ShadowIntProperty").CurrentValue = 42;

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithShadowProperties.FirstOrDefaultAsync();
      loadedEntity.Should().NotBeNull();
      AssertDbContext.Entry(loadedEntity!).Property("ShadowStringProperty").CurrentValue.Should().Be("value");
      AssertDbContext.Entry(loadedEntity!).Property("ShadowIntProperty").CurrentValue.Should().Be(42);
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

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity }, new SqliteBulkUpdateOptions());

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

      var affectedRows = await SUT.BulkUpdateAsync(new[] { entity },
                                                   new SqliteBulkUpdateOptions
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
