using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using MySqlConnector;

namespace Thinktecture.EntityFrameworkCore.Parameters;

[Collection(MySqlTestCollection.NAME)]
public sealed class CollectionParameterExecutionTests : MySqlIntegrationTestsBase
{
   public CollectionParameterExecutionTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_round_trip_decimals_with_more_than_15_significant_digits()
   {
      await using var context = CreateContext();

      var result = await context.CreateScalarCollectionParameter(new[] { 12345678901234567.89m }, applyDistinct: false).ToListAsync();

      result.Should().Equal(12345678901234567.89m);
   }

   [Fact]
   public async Task Should_round_trip_time_spans_of_more_than_a_day()
   {
      await using var context = CreateContext();
      var value = TimeSpan.FromHours(36);

      var result = await context.CreateScalarCollectionParameter(new[] { value, TimeSpan.FromMinutes(-90) }, applyDistinct: false).ToListAsync();

      result.Should().BeEquivalentTo(new[] { value, TimeSpan.FromMinutes(-90) });
   }

   [Fact]
   public async Task Should_round_trip_date_times_with_fractional_seconds()
   {
      await using var context = CreateContext();
      var value = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(1234560);

      var result = await context.CreateScalarCollectionParameter(new[] { value }, applyDistinct: false).ToListAsync();

      result.Should().Equal(value);
   }

   [Fact]
   public async Task Should_round_trip_common_scalar_types()
   {
      await using var context = CreateContext();
      var guid = Guid.NewGuid();

      (await context.CreateScalarCollectionParameter(new[] { 1, 2, 3 }, applyDistinct: false).OrderBy(value => value).ToListAsync()).Should().Equal(1, 2, 3);
      (await context.CreateScalarCollectionParameter(new[] { 1L << 40 }, applyDistinct: false).ToListAsync()).Should().Equal(1L << 40);
      (await context.CreateScalarCollectionParameter(new[] { "a", "b" }, applyDistinct: false).OrderBy(value => value).ToListAsync()).Should().Equal("a", "b");
      (await context.CreateScalarCollectionParameter(new[] { guid }, applyDistinct: false).ToListAsync()).Should().Equal(guid);
      (await context.CreateScalarCollectionParameter(new[] { 1.5d }, applyDistinct: false).ToListAsync()).Should().Equal(1.5d);
   }

   [Fact]
   public async Task Should_round_trip_boolean_and_date_time_offset_values()
   {
      await using var context = CreateContext();
      var value = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)).AddTicks(1234560);

      (await context.CreateScalarCollectionParameter(new[] { true, false }, applyDistinct: false).OrderBy(item => item).ToListAsync()).Should().Equal(false, true);
      (await context.CreateScalarCollectionParameter(new[] { value }, applyDistinct: false).ToListAsync()).Should().Equal(value);
   }

   [Fact]
   public async Task Should_match_date_time_offset_column_when_session_time_zone_is_not_utc()
   {
      ConfigureMySql = builder => builder.AddCollectionParameterSupport();
      var value = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)).AddTicks(1234560);
      ArrangeDbContext.TemporalEntities.Add(new MySqlTemporalEntity
                                            {
                                               Id = Guid.NewGuid(),
                                               DateTime = new DateTime(2000, 1, 1),
                                               DateTimeOffset = value,
                                               TimeOnly = new TimeOnly(1, 0),
                                               TimeSpan = TimeSpan.FromHours(1),
                                               NullableDateTime = new DateTime(2000, 1, 1),
                                               Converted = new MySqlDateTimeWrapper(new DateTime(2000, 1, 1))
                                            });
      await ArrangeDbContext.SaveChangesAsync();

      // The session time zone belongs to the connection, so the connection stays open. The pool resets it afterwards.
      await ActDbContext.Database.OpenConnectionAsync();
      await ActDbContext.Database.ExecuteSqlRawAsync("SET time_zone = '+05:00'");
      var values = ActDbContext.CreateScalarCollectionParameter(new[] { value }, applyDistinct: false);

      (await ActDbContext.TemporalEntities.Where(e => values.Contains(e.DateTimeOffset)).CountAsync()).Should().Be(1);
   }

   [Fact]
   public async Task Should_not_request_the_design_time_model_for_a_collection_parameter_without_string_columns()
   {
      Configure = builder => builder.ReplaceService<IDesignTimeModel, ThrowingDesignTimeModel>();
      ConfigureMySql = builder => builder.AddCollectionParameterSupport();

      var values = await ActDbContext.CreateScalarCollectionParameter(new[] { 1, 2 }, applyDistinct: false).OrderBy(value => value).ToListAsync();

      values.Should().Equal(1, 2);
   }

   [Fact]
   public async Task Should_apply_distinct()
   {
      await using var context = CreateContext();

      var result = await context.CreateScalarCollectionParameter(new[] { 1, 1, 2 }, applyDistinct: true).OrderBy(value => value).ToListAsync();

      result.Should().Equal(1, 2);
   }

   [Fact]
   public async Task Should_join_a_scalar_collection_with_a_table()
   {
      await using var context = await CreateContextAsync(builder => builder.AddCollectionParameterSupport());
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "a" }, new MySqlTestEntity { Id = 2, Name = "b" }, new MySqlTestEntity { Id = 3, Name = "c" }]);
      var ids = context.CreateScalarCollectionParameter(new[] { 1, 3 }, applyDistinct: true);

      var names = await context.Entities.Where(entity => ids.Contains(entity.Id)).OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync();

      names.Should().Equal("a", "c");
   }

   [Fact]
   public async Task Should_read_complex_objects_using_the_json_naming_policy()
   {
      await using var context = CreateContext(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      var result = await context.CreateComplexCollectionParameter(new[] { new MySqlTestComplexParameter(7), new MySqlTestComplexParameter(8) }, applyDistinct: false)
                                .OrderBy(parameter => parameter.Id)
                                .ToListAsync();

      result.Select(parameter => parameter.Id).Should().Equal(7, 8);
   }

   [Fact]
   public async Task Should_read_complex_objects_using_json_property_name_attributes()
   {
      await using var context = CreateContext();

      var result = await context.CreateComplexCollectionParameter(new[] { new MySqlTestJsonNamedComplexParameter(7) }, applyDistinct: false)
                                .SingleAsync();

      result.Value.Should().Be(7);
   }

   [Fact]
   public async Task Should_apply_distinct_to_complex_collection_parameters()
   {
      await using var context = CreateContext();

      var result = await context.CreateComplexCollectionParameter(
                                                                  [new MySqlTestComplexParameter(7), new MySqlTestComplexParameter(7)],
                                                                  applyDistinct: true)
                                .ToListAsync();

      result.Should().ContainSingle().Which.Id.Should().Be(7);
   }

   [Fact]
   public async Task Should_throw_if_json_value_cannot_be_converted_to_column_type()
   {
      ConfigureModel = builder => builder.ConfigureScalarCollectionParameter<string>(b => b.Property(e => e.Value).HasMaxLength(3));
      ConfigureMySql = builder => builder.AddCollectionParameterSupport();

      var query = ActDbContext.CreateScalarCollectionParameter(new[] { "abc", "abcdef" }, applyDistinct: false);

      // Error 3669: "Value is out of range for JSON_TABLE's column".
      (await query.Awaiting(q => q.ToListAsync()).Should().ThrowAsync<MySqlException>()).Which.Number.Should().Be(3669);
   }

   [Fact]
   public async Task Should_compare_string_collection_parameter_with_collated_column()
   {
      // Two non-binary collations of the same charset fail a comparison with error 1267 unless the JSON_TABLE column has the collation of the table column.
      ConfigureModel = builder =>
                       {
                          builder.Entity<UnicodeCollatedEntity>(b =>
                                                                {
                                                                   b.ToTable(UnicodeCollatedEntity.TABLE_NAME);
                                                                   b.Property(e => e.Name).HasMaxLength(100).UseCollation("utf8mb4_unicode_ci");
                                                                });
                          builder.ConfigureScalarCollectionParameter<string>(b => b.Property(e => e.Value).UseCollation("utf8mb4_unicode_ci"));
                       };
      ConfigureMySql = builder => builder.AddCollectionParameterSupport();

      await ArrangeDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE `{UnicodeCollatedEntity.TABLE_NAME}` (`Id` int NOT NULL PRIMARY KEY, `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL)");

      try
      {
         await ArrangeDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO `{UnicodeCollatedEntity.TABLE_NAME}` (`Id`, `Name`) VALUES (1, 'Value'), (2, 'Other')");

         var names = ActDbContext.CreateScalarCollectionParameter(new[] { "VALUE" }, applyDistinct: false);
         var ids = await ActDbContext.Set<UnicodeCollatedEntity>().Where(entity => names.Contains(entity.Name)).Select(entity => entity.Id).ToListAsync();

         ids.Should().Equal(1);
         ExecutedCommands.Should().ContainSingle(command => command.Contains("JSON_TABLE", StringComparison.Ordinal))
                         .Which.Should().Contain("COLLATE utf8mb4_unicode_ci");
      }
      finally
      {
         await ArrangeDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{UnicodeCollatedEntity.TABLE_NAME}`");
      }
   }

   private MySqlTestDbContext CreateContext(JsonSerializerOptions? jsonSerializerOptions = null)
   {
      var options = MySqlTestDbContext.CreateOptionsBuilder()
                    .UseMySql(ConnectionString, MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport().AddCollectionParameterSupport(jsonSerializerOptions))
                    .Options;

      return new MySqlTestDbContext(options);
   }

   private sealed class UnicodeCollatedEntity
   {
      public const string TABLE_NAME = "collection_parameter_unicode_collated";

      public int Id { get; set; }
      public string Name { get; set; } = null!;
   }
}
