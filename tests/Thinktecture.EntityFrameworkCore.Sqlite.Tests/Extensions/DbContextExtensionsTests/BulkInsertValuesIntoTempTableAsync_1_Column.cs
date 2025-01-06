using Microsoft.Data.Sqlite;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertValuesIntoTempTableAsync_1_Column : SchemaChangingIntegrationTestsBase
{
   public BulkInsertValuesIntoTempTableAsync_1_Column(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_insert_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new List<int> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values);

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { 1, 2 });
   }

   [Fact]
   public async Task Should_insert_int_into_non_empty_temp_table()
   {
      // Arrange
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new List<int> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values);

      // Act
      await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 3, 4 }, query);

      // Assert
      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo([1, 2, 3, 4]);
   }

   [Fact]
   public async Task Should_throw_if_inserting_duplicates()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>(false);

      var values = new List<int> { 1, 1 };
      await ActDbContext.Awaiting(ctx => ctx.BulkInsertValuesIntoTempTableAsync(values))
                        .Should().ThrowAsync<SqliteException>().Where(ex => ex.Message.StartsWith("SQLite Error 19: 'UNIQUE constraint failed: #TempTable<int>_1.Column1'.", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_insert_nullable_int_of_a_keyless_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      var values = new List<int?> { 1, null };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqliteTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { 1, (int?)null });
   }

   [Fact]
   public async Task Should_insert_nullable_int_of_entity_with_a_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>(false,
                                                                   entityBuilder =>
                                                                   {
                                                                      entityBuilder.HasKey(t => t.Column1);
                                                                      entityBuilder.Property(t => t.Column1).ValueGeneratedNever();
                                                                   });

      var values = new List<int?> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqliteTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { 1, 2 });
   }

   [Fact]
   public async Task Should_insert_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string?>(entityBuilder => entityBuilder.Property(t => t.Column1).IsRequired(false));

      var values = new List<string?> { "value1", null };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqliteTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { "value1", null });
   }

   [Fact]
   public async Task Should_create_pk_by_default_on_string_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(entityBuilder => entityBuilder.Property(t => t.Column1).HasMaxLength(100).IsRequired());

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<string> { "value" }, new SqliteTempTableBulkInsertOptions
                                                                                                                      {
                                                                                                                         TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                         PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced
                                                                                                                      });

      var keys = AssertDbContext.GetTempTableKeyColumns<TempTable<string>>().ToList();
      keys.Should().HaveCount(1);
      keys[0].Name.Should().Be(nameof(TempTable<string>.Column1));
   }

   [Fact]
   public async Task Should_create_pk_on_nullable_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int?> { 1 }, new SqliteTempTableBulkInsertOptions
                                                                                  {
                                                                                     TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                     PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced
                                                                                  });

      var keys = AssertDbContext.GetTempTableKeyColumns<TempTable<int?>>().ToList();
      keys.Should().HaveCount(1);
      keys[0].Name.Should().Be(nameof(TempTable<int?>.Column1));
   }

   [Fact]
   public async Task Should_create_pk_by_default()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new SqliteTempTableBulkInsertOptions
                                                                                                             {
                                                                                                                TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced
                                                                                                             });

      var keys = ArrangeDbContext.GetTempTableKeyColumns<TempTable<int>>().ToList();
      keys.Should().HaveCount(1);
      keys[0].Name.Should().Be(nameof(TempTable<int>.Column1));
   }

   [Fact]
   public async Task Should_not_create_pk_if_entity_is_keyless_and_provider_is_AccordingToEntityTypeConfiguration()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new SqliteTempTableBulkInsertOptions
                                                                                                             {
                                                                                                                TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration
                                                                                                             });

      var keys = ArrangeDbContext.GetTempTableKeyColumns<TempTable<int>>().ToList();
      keys.Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_not_create_pk_if_specified_in_options()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new SqliteTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var keys = ArrangeDbContext.GetTempTableKeyColumns<TempTable<int>>().ToList();
      keys.Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_work_with_projection_using_SplitQuery_behavior()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var parentId = new Guid("6EB48130-454B-46BC-9FEE-CDF411F69807");
      var childId = new Guid("17A2014C-F7B2-40E8-82F4-42E754DCAA2D");
      ArrangeDbContext.TestEntities.Add(new TestEntity
                                        {
                                           Id = parentId,
                                           RequiredName = "RequiredName",
                                           Children = { new() { Id = childId, RequiredName = "RequiredName" } }
                                        });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { parentId });

      var query = await ActDbContext.TestEntities
                                    .AsSplitQuery()
                                    .Where(c => tempTable.Query.Any(id => c.Id == id))
                                    .Select(c => new
                                                 {
                                                    c.Id,
                                                    ChildrenIds = c.Children.Select(o => o.Id).ToList()
                                                 })
                                    .ToListAsync();

      query.Should().BeEquivalentTo(new[]
                                    {
                                       new
                                       {
                                          Id = parentId,
                                          ChildrenIds = new List<Guid> { childId }
                                       }
                                    });
   }

   [Fact]
   public async Task Should_join_with_itself()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { Guid.Empty });

      var joinQuery = tempTable.Query.LeftJoin(tempTable.Query, e => e, e => e);

      joinQuery.ToQueryString().Should().Be("""
                                               SELECT "#"."Column1" AS "Left", "#0"."Column1" AS "Right"
                                               FROM "#TempTable<Guid>_1" AS "#"
                                               LEFT JOIN "#TempTable<Guid>_1" AS "#0" ON "#"."Column1" = "#0"."Column1"
                                               """.WithEnvironmentLineBreaks());
   }
}
