using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateTempTableEntityAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;
   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateTempTableEntityAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_throw_when_trying_to_update_temp_table_entity_without_table_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await SUT.Invoking(sut => sut.BulkUpdateAsync(new List<TempTable<int>> { new(0) }, new SqliteBulkUpdateOptions()))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }

   [Fact]
   public async Task Should_update_temp_table_entity_when_table_name_is_provided()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entity = new TestEntity
                   {
                      Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42
                   };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntityTempTable
                       {
                          Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                          Name = "UpdatedName",
                          RequiredName = "RequiredName",
                          Count = 99
                       };

      var affectedRows = await SUT.BulkUpdateAsync(new[] { tempEntity }, new SqliteBulkUpdateOptions { TableName = "TestEntities" });

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
      loadedEntities[0].Id.Should().Be(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loadedEntities[0].Name.Should().Be("UpdatedName");
      loadedEntities[0].Count.Should().Be(99);
   }
}
