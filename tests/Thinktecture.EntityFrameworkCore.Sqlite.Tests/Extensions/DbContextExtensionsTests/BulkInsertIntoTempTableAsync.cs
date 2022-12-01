using Microsoft.Data.Sqlite;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable InconsistentNaming
public class BulkInsertIntoTempTableAsync : IntegrationTestsBase
{
   public BulkInsertIntoTempTableAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_insert_keyless_type()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(typeBuilder => typeBuilder.Property(t => t.Column2).HasMaxLength(100).IsRequired());

      var entities = new List<CustomTempTable> { new(1, "value") };
      await using var query = await ActDbContext.BulkInsertIntoTempTableAsync(entities);

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { new CustomTempTable(1, "value") });
   }

   [Fact]
   public async Task Should_insert_entityType_without_touching_real_table()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                      Name = "Name",
                      RequiredName = "RequiredName",
                      Count = 42,
                      ConvertibleClass = new ConvertibleClass(43)
                   };
      ArrangeDbContext.TestEntities.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var entities = new List<TestEntity> { entity };
      await using var query = await ActDbContext.BulkInsertIntoTempTableAsync(entities);

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[]
                                        {
                                           new TestEntity
                                           {
                                              Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                                              Name = "Name",
                                              RequiredName = "RequiredName",
                                              Count = 42,
                                              ConvertibleClass = new ConvertibleClass(43)
                                           }
                                        });
   }

   [Fact]
   public async Task Should_insert_entityType_without_required_fields_if_excluded_and_with_UsePropertiesToInsertForTempTableCreation()
   {
      var entity = new TestEntity
                   {
                      Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730")
                   };
      var entities = new List<TestEntity> { entity };
      await using var query = await ActDbContext.BulkInsertIntoTempTableAsync(entities, new SqliteTempTableBulkInsertOptions
                                                                                        {
                                                                                           PropertiesToInsert = IEntityPropertiesProvider.Include<TestEntity>(e => e.Id),
                                                                                           Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                                                                                        });

      var tempTable = await query.Query.Select(t => new { t.Id }).ToListAsync();
      tempTable.Should().BeEquivalentTo(new[]
                                        {
                                           new { Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730") }
                                        });
   }

   [Fact]
   public async Task Should_return_disposable_query()
   {
      await using var tempTableQuery = await ActDbContext.BulkInsertIntoTempTableAsync(Array.Empty<TestEntity>());
      var query = tempTableQuery.Query;
      tempTableQuery.Dispose();

      await query.Awaiting(q => q.ToListAsync())
                 .Should().ThrowAsync<SqliteException>().WithMessage("SQLite Error 1: 'no such table: TestEntities_1'.");
   }

   [Fact]
   public async Task Should_return_detached_entities_for_entities_with_a_primary_key()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                          RequiredName = "Name1"
                       };

      await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity });

      var entities = await tempTable.Query.ToListAsync();

      ActDbContext.Entry(entities[0]).State.Should().Be(EntityState.Detached);
   }

   [Fact]
   public async Task Should_not_mess_up_temp_tables_with_alternating_requests_without_disposing_previous_one()
   {
      var testEntity1 = new TestEntity
                        {
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name1"
                        };
      var testEntity2 = new TestEntity
                        {
                           Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                           RequiredName = "Name2"
                        };

      await using var tempTable1_1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 });
      await using var tempTable2_1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 });
      await using var tempTable1_2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 });
      await using var tempTable2_2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 });

      tempTable1_1.Query.ToList().Should().BeEquivalentTo(new[] { testEntity1 });
      tempTable1_2.Query.ToList().Should().BeEquivalentTo(new[] { testEntity1 });
      tempTable2_1.Query.ToList().Should().BeEquivalentTo(new[] { testEntity2 });
      tempTable2_2.Query.ToList().Should().BeEquivalentTo(new[] { testEntity2 });
   }

   [Fact]
   public async Task Should_not_mess_up_temp_tables_with_alternating_requests_with_disposing_previous_one()
   {
      var testEntity1 = new TestEntity { Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), RequiredName = "Name1" };
      var testEntity2 = new TestEntity { Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), RequiredName = "Name1" };

      await using (var tempTable1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 }))
      await using (var tempTable2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 }))
      {
         tempTable1.Query.ToList().Should().BeEquivalentTo(new[] { testEntity1 });
         tempTable2.Query.ToList().Should().BeEquivalentTo(new[] { testEntity2 });
      }

      await using (var tempTable1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 }))
      await using (var tempTable2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 }))
      {
         tempTable1.Query.ToList().Should().BeEquivalentTo(new[] { testEntity1 });
         tempTable2.Query.ToList().Should().BeEquivalentTo(new[] { testEntity2 });
      }
   }

   [Fact]
   public async Task Should_properly_join_real_table_with_temp_table()
   {
      var realEntity = new TestEntity
                       {
                          Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                          RequiredName = "Name1"
                       };
      ArrangeDbContext.Add(realEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var tempEntity = new TestEntity
                       {
                          Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                          RequiredName = "Name2"
                       };

      await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { tempEntity });

      var entities = await tempTable.Query
                                    .Join(ActDbContext.TestEntities, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                    .ToListAsync();

      entities.Should().BeEquivalentTo(new[] { new { temp = tempEntity, real = realEntity } });

      entities = await tempTable.Query
                                .Join(ActDbContext.TestEntities, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                .ToListAsync();

      entities.Should().BeEquivalentTo(new[] { new { temp = tempEntity, real = realEntity } });
   }
}
