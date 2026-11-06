using System.Text.Json;

namespace Thinktecture.EntityFrameworkCore.Parameters;

[Collection(MySqlTestCollection.NAME)]
public sealed class CollectionParameterExecutionTests : MySqlIntegrationTestsBase
{
   public CollectionParameterExecutionTests(MySqlContainerFixture fixture)
      : base(fixture)
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

   private MySqlTestDbContext CreateContext(JsonSerializerOptions? jsonSerializerOptions = null)
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql(ConnectionString, MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport().AddCollectionParameterSupport(jsonSerializerOptions))
                    .Options;

      return new MySqlTestDbContext(options);
   }
}
