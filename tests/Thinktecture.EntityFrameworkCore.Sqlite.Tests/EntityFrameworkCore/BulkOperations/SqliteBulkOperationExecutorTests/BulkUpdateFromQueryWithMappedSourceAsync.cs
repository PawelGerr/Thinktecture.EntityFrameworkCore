using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryWithMappedSourceAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor SUT => field ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateFromQueryWithMappedSourceAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", Count = 42 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder
            .Set(e => e.Name, (e, f) => f.Name)
            .Set(e => e.Count, (e, f) => f.Count));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Name.Should().Be("Updated");
      loaded.Count.Should().Be(42);
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_using_arithmetic_expression()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 5 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated", Count = 3 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => e.Count + f.Count));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(8);
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_filter()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity_1 = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original1", RequiredName = "RequiredName", Count = 5 };
      var entity_2 = new TestEntity { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Original2", RequiredName = "RequiredName", Count = 20 };
      ArrangeDbContext.AddRange(entity_1, entity_2);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Updated1", Count = 10 },
         new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Updated2", Count = 10 }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

      // Only update where target Count < source Count (entity_1: 5 < 10 = true, entity_2: 20 < 10 = false)
      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Name, (e, f) => f.Name),
         filter: (e, f) => e.Count < f.Count);

      affectedRows.Should().Be(1);

      var loaded_1 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"));
      loaded_1.Name.Should().Be("Updated1");

      var loaded_2 = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"));
      loaded_2.Name.Should().Be("Original2");
   }

   [Fact]
   public async Task Should_update_from_source_temp_table_with_different_column_names_and_constant_value()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
      {
         b.HasKey(e => e.Id);
         b.Property(e => e.Id).HasColumnName("entity_id");
         b.Property(e => e.Name).HasColumnName("display_name");
         b.Property(e => e.Count).HasColumnName("item_count");
      });

      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Original", RequiredName = "RequiredName", Count = 0 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      var sourceEntities = new List<TempSourceWithMappedColumns>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866") }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntity>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         f => f.Id,
         builder => builder.Set(e => e.Count, (e, f) => 42));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities.SingleAsync();
      loaded.Count.Should().Be(42);
   }
}
