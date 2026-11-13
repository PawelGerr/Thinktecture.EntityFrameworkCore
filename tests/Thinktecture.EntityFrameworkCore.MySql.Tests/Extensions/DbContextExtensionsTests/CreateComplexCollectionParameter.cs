using System.Text.Json;
using System.Text.Json.Serialization;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// The objects are sent as one JSON array and read with JSON_TABLE; one JSON_TABLE column per property, addressed by its JSON property name
// (attribute [JsonPropertyName] first, then the naming policy of the JsonSerializerOptions, then the property name).
[Collection(MySqlTestCollection.NAME)]
public sealed class CreateComplexCollectionParameter : MySqlIntegrationTestsBase
{
   private static readonly Guid _TEST_ENTITY_ID = new("7F8B0E79-2C91-4682-9F61-6FC86B4E5244");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("D3E99F44-40A1-4E4E-820F-9D7C7B02AFA5");

   public CreateComplexCollectionParameter(MySqlContainerFixture fixture)
      : base(fixture)
   {
      ConfigureMySql = builder => builder.AddCollectionParameterSupport(CollectionParameterJsonSerializerOptions.Default);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_do_roundtrip(bool applyDistinct)
   {
      var result = await ActDbContext.CreateComplexCollectionParameter([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))], applyDistinct)
                                     .ToListAsync();

      result.Should().BeEquivalentTo([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))]);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_work_with_joins(bool applyDistinct)
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          ConvertibleClass = new ConvertibleClass(42)
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.CreateComplexCollectionParameter([new MyParameter(testEntity.Id, new ConvertibleClass(42))], applyDistinct);
      var loadedEntities = await ActDbContext.TestEntities
                                             .Join(collectionParameter, t => new { t.Id, ConvertibleClass = t.ConvertibleClass! }, p => new { Id = p.Column1, ConvertibleClass = p.Column2 }, (t, p) => t)
                                             .ToListAsync();

      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Name = "Name",
                                              RequiredName = "RequiredName",
                                              ConvertibleClass = new ConvertibleClass(42)
                                           });
   }

   [Fact]
   public async Task Should_not_find_rows_if_one_of_the_joined_columns_differs()
   {
      await ArrangeDbContext.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(42) }]);

      var collectionParameter = ActDbContext.CreateComplexCollectionParameter([new MyParameter(_TEST_ENTITY_ID, new ConvertibleClass(43)), new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))]);
      var loadedEntities = await ActDbContext.TestEntities
                                             .Join(collectionParameter, t => new { t.Id, ConvertibleClass = t.ConvertibleClass! }, p => new { Id = p.Column1, ConvertibleClass = p.Column2 }, (t, p) => t)
                                             .ToListAsync();

      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_filter_a_table_using_any_on_the_parameter()
   {
      await ArrangeDbContext.BulkInsertAsync([
                                                new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "first", ConvertibleClass = new ConvertibleClass(1) },
                                                new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "second", ConvertibleClass = new ConvertibleClass(2) }
                                             ]);

      var collectionParameter = ActDbContext.CreateComplexCollectionParameter([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(2)), new MyParameter(Guid.NewGuid(), new ConvertibleClass(1))]);

      var result = await ActDbContext.TestEntities
                                     .Where(t => collectionParameter.Any(p => p.Column1 == t.Id))
                                     .Select(t => t.RequiredName)
                                     .ToListAsync();

      result.Should().Equal("second");
   }

   [Fact]
   public async Task Should_return_all_objects_without_distinct_and_unique_objects_with_distinct()
   {
      var first = new MyParameter(_TEST_ENTITY_ID, new ConvertibleClass(1));
      var second = new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(2));
      var collection = new[] { first, second, first };

      var withDistinct = await ActDbContext.CreateComplexCollectionParameter(collection, applyDistinct: true).ToListAsync();
      var withoutDistinct = await ActDbContext.CreateComplexCollectionParameter(collection, applyDistinct: false).ToListAsync();

      withDistinct.Should().BeEquivalentTo([first, second]);
      withoutDistinct.Should().BeEquivalentTo([first, second, first]);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_return_empty_result_for_empty_collection(bool applyDistinct)
   {
      var result = await ActDbContext.CreateComplexCollectionParameter(Array.Empty<MyParameter>(), applyDistinct).ToListAsync();

      result.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_work_with_large_collection()
   {
      var objects = Enumerable.Range(0, 20_000).Select(i => new MyParameter(new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), new ConvertibleClass(i))).ToList();

      var query = ActDbContext.CreateComplexCollectionParameter(objects, applyDistinct: false);

      (await query.CountAsync()).Should().Be(20_000);
      (await query.OrderByDescending(p => p.Column2).Select(p => p.Column2).FirstAsync()).Should().Be(new ConvertibleClass(19_999));
   }

   [Fact]
   public async Task Should_work_with_large_collection_with_distinct()
   {
      var objects = Enumerable.Range(0, 20_000).Select(i => new MyParameter(new Guid(i % 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), new ConvertibleClass(i % 100))).ToList();

      (await ActDbContext.CreateComplexCollectionParameter(objects, applyDistinct: true).CountAsync()).Should().Be(100);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_work_with_all_column_types(bool applyDistinct)
   {
      ConfigureModel = builder => builder.ConfigureComplexCollectionParameter<ComplexParameterWithAllTypes>();

      var result = await ActDbContext.CreateComplexCollectionParameter(CreateAllTypes(), applyDistinct).ToListAsync();

      result.Should().BeEquivalentTo(CreateAllTypes());
   }

   [Fact]
   public async Task Should_filter_a_table_by_columns_of_all_types()
   {
      ConfigureModel = builder => builder.ConfigureComplexCollectionParameter<ComplexParameterWithAllTypes>();

      await ArrangeDbContext.BulkInsertAsync([
                                                new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "first", Name = "it's \"quoted\" {0} äöü", Count = 1 },
                                                new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "second", Name = "", Count = 2 }
                                             ]);

      var collectionParameter = ActDbContext.CreateComplexCollectionParameter(CreateAllTypes());

      var result = await ActDbContext.TestEntities
                                     .Join(collectionParameter, t => new { t.Name, t.Count }, p => new { Name = (string?)p.StringValue, Count = p.IntValue }, (t, p) => new { t.RequiredName, p.TimeSpanValue })
                                     .OrderBy(t => t.RequiredName)
                                     .ToListAsync();

      result.Should().BeEquivalentTo([
                                        new { RequiredName = "first", TimeSpanValue = TimeSpan.FromHours(36) },
                                        new { RequiredName = "second", TimeSpanValue = TimeSpan.FromMinutes(-90) }
                                     ],
                                     options => options.WithStrictOrdering());
   }

   // The EF service provider depends on the JsonSerializerOptions instance, so the naming policies are limited to a few shared instances.
   [Theory]
   [InlineData("None")]
   [InlineData("CamelCase")]
   [InlineData("SnakeCaseLower")]
   public async Task Should_read_objects_using_the_json_naming_policy(string namingPolicy)
   {
      ConfigureMySql = builder => builder.AddCollectionParameterSupport(GetJsonSerializerOptions(namingPolicy));
      ConfigureModel = builder => builder.ConfigureComplexCollectionParameter<ComplexParameterWithAllTypes>();

      var allTypes = await ActDbContext.CreateComplexCollectionParameter(CreateAllTypes()).ToListAsync();
      var withConverter = await ActDbContext.CreateComplexCollectionParameter([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))]).ToListAsync();

      allTypes.Should().BeEquivalentTo(CreateAllTypes());
      withConverter.Should().BeEquivalentTo([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))]);
   }

   [Theory]
   [InlineData("None")]
   [InlineData("CamelCase")]
   public async Task Should_read_objects_using_json_property_name_attributes(string namingPolicy)
   {
      // The attribute wins over the naming policy.
      ConfigureMySql = builder => builder.AddCollectionParameterSupport(GetJsonSerializerOptions(namingPolicy));
      ConfigureModel = builder => builder.ConfigureComplexCollectionParameter<JsonNamedComplexParameter>();

      var objects = new[]
                    {
                       new JsonNamedComplexParameter("John", "Doe Jr.", 1, 10),
                       new JsonNamedComplexParameter("Jane", "O'Neil", 2, 20)
                    };

      var result = await ActDbContext.CreateComplexCollectionParameter(objects).ToListAsync();

      result.Should().BeEquivalentTo(objects);
   }

   [Fact]
   public async Task Should_join_objects_having_json_property_name_attributes_with_a_table()
   {
      ConfigureModel = builder => builder.ConfigureComplexCollectionParameter<JsonNamedComplexParameter>();

      await ArrangeDbContext.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "first", Count = 2 }, new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "second", Count = 3 }]);

      var collectionParameter = ActDbContext.CreateComplexCollectionParameter([
                                                                                 new JsonNamedComplexParameter("John", "Doe", 1, 10),
                                                                                 new JsonNamedComplexParameter("Jane", "Roe", 2, 20)
                                                                              ]);

      var result = await ActDbContext.TestEntities
                                     .Join(collectionParameter, t => t.Count, p => p.Quoted, (t, p) => new { t.RequiredName, p.FirstName })
                                     .ToListAsync();

      result.Should().BeEquivalentTo([new { RequiredName = "first", FirstName = "Jane" }]);
   }

   [Fact]
   public async Task Should_read_the_collection_with_json_table()
   {
      var result = await ActDbContext.CreateComplexCollectionParameter([new MyParameter(_OTHER_TEST_ENTITY_ID, new ConvertibleClass(42))]).ToListAsync();

      result.Should().ContainSingle();
      var command = ExecutedCommands.Should().ContainSingle(c => c.Contains("JSON_TABLE", StringComparison.Ordinal)).Subject;
      command.Should().Contain("PATH '$.\"Column1\"'").And.Contain("PATH '$.\"Column2\"'").And.Contain("AS `Id`");
   }

   private static JsonSerializerOptions GetJsonSerializerOptions(string namingPolicy)
   {
      return namingPolicy switch
      {
         "None" => CollectionParameterJsonSerializerOptions.Default,
         "CamelCase" => CollectionParameterJsonSerializerOptions.CamelCase,
         "SnakeCaseLower" => CollectionParameterJsonSerializerOptions.SnakeCaseLower,
         _ => throw new ArgumentOutOfRangeException(nameof(namingPolicy), namingPolicy, null)
      };
   }

   private static ComplexParameterWithAllTypes[] CreateAllTypes()
   {
      return
      [
         new ComplexParameterWithAllTypes(1,
                                          1L << 40,
                                          -5,
                                          200,
                                          true,
                                          12345678901234567.89m,
                                          4.2d,
                                          4.3f,
                                          "it's \"quoted\" {0} äöü",
                                          new Guid("CE5E3D80-13D6-43C3-BB41-6499E2AD6B63"),
                                          new DateTime(2021, 1, 15, 12, 30, 40).AddTicks(1234560),
                                          new DateTimeOffset(new DateTime(2021, 1, 15, 12, 30, 40), TimeSpan.FromMinutes(60)),
                                          TimeSpan.FromHours(36),
                                          7,
                                          "text"),
         new ComplexParameterWithAllTypes(2,
                                          -1,
                                          5,
                                          0,
                                          false,
                                          -0.5m,
                                          -4.2d,
                                          -4.3f,
                                          "",
                                          Guid.Empty,
                                          new DateTime(1000, 1, 1),
                                          new DateTimeOffset(2020, 2, 29, 1, 2, 3, TimeSpan.Zero),
                                          TimeSpan.FromMinutes(-90),
                                          null,
                                          null)
      ];
   }
}

internal sealed record ComplexParameterWithAllTypes(
   int IntValue,
   long LongValue,
   short ShortValue,
   byte ByteValue,
   bool BoolValue,
   decimal DecimalValue,
   double DoubleValue,
   float FloatValue,
   string StringValue,
   Guid GuidValue,
   DateTime DateTimeValue,
   DateTimeOffset DateTimeOffsetValue,
   TimeSpan TimeSpanValue,
   int? NullableIntValue,
   string? NullableStringValue);

// JSON property names containing a dot, a space, a single quote and a double quote.
internal sealed record JsonNamedComplexParameter(
   [property: JsonPropertyName("first.name")]
   string FirstName,
   [property: JsonPropertyName("last name")]
   string LastName,
   [property: JsonPropertyName("it's \"quoted\"")]
   int Quoted,
   int PlainValue);
