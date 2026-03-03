using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertOrUpdateTempTableEntityAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;
   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkInsertOrUpdateTempTableEntityAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_throw_when_trying_to_insert_or_update_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Invoking(sut => sut.BulkInsertOrUpdateAsync(new List<TempTable<int>> { new(0) }, new SqliteBulkInsertOrUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_insert_or_update_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var existingEntity = new TestEntity
                           {
                              Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                              Name = "Name",
                              RequiredName = "RequiredName",
                              Count = 42
                           };
      ArrangeDbContext.Add(existingEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var updatedEntity = new TestEntityTempTable
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "UpdatedName",
                             RequiredName = "RequiredName",
                             Count = 99
                          };
      var newEntity = new TestEntityTempTable
                      {
                         Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"),
                         Name = "NewName",
                         RequiredName = "RequiredName",
                         Count = 1
                      };

      var affectedRows = await SUT.BulkInsertOrUpdateAsync(new[] { updatedEntity, newEntity }, new SqliteBulkInsertOrUpdateOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Id.Should().Be(new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loadedEntities[0].Name.Should().Be("NewName");
      loadedEntities[0].Count.Should().Be(1);
      loadedEntities[1].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[1].Name.Should().Be("UpdatedName");
      loadedEntities[1].Count.Should().Be(99);
   }
}
