using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// The collection is sent as one JSON parameter and read with JSON_TABLE; the column types come from the type mapping of the model.
// MySQL parses JSON numbers as DOUBLE and rejects the .NET formats of decimal, TimeSpan and DateTime, so these types are sent as strings.
[Collection(MySqlTestCollection.NAME)]
public sealed class CreateScalarCollectionParameter : MySqlIntegrationTestsBase
{
   private static readonly MethodInfo _genericRoundtripTest = typeof(CreateScalarCollectionParameter).GetMethod(nameof(AssertGenericRoundtripAsync), BindingFlags.Instance | BindingFlags.NonPublic)
                                                              ?? throw new Exception($"Method '{nameof(AssertGenericRoundtripAsync)}' not found.");

   private static readonly Guid _TEST_ENTITY_ID = new("7F8B0E79-2C91-4682-9F61-6FC86B4E5244");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("D3E99F44-40A1-4E4E-820F-9D7C7B02AFA5");

   // The default data types, one value each.
   public static readonly IEnumerable<object[]> Values =
   [
      [new[] { 42 }],
      [new[] { 43L }],
      [new[] { new DateTime(2021, 1, 15, 12, 30, 40) }],
      [new[] { new Guid("CE5E3D80-13D6-43C3-BB41-6499E2AD6B63") }],
      [new[] { true }],
      [new[] { (byte)4 }],
      [new[] { 4.2d }],
      [new[] { new DateTimeOffset(new DateTime(2021, 1, 15, 12, 30, 40), TimeSpan.FromMinutes(60)) }],
      [new[] { (short)5 }],
      [new[] { 4.3f }],
      [new[] { 4.4m }],
      [new[] { TimeSpan.FromMinutes(60) }],
      [new[] { "test" }],
      [new[] { new ConvertibleClass(99) }],
   ];

   // Several distinct values per type, with and without DISTINCT.
   public static readonly IEnumerable<object[]> MultipleValues = WithDistinctFlags(
   [
      new[] { 1, 2, 3, -4 },
      new[] { 1L, 1L << 40, -5L },
      new[] { (short)-5, (short)5, (short)0 },
      new[] { (byte)0, (byte)1, (byte)255 },
      new[] { true, false },
      new[] { "a", "b", "" },
      new[] { new Guid("CE5E3D80-13D6-43C3-BB41-6499E2AD6B63"), new Guid("D3E99F44-40A1-4E4E-820F-9D7C7B02AFA5") },
      new[] { 4.4m, -1.5m, 0m, 12345678901234567.89m },
      new[] { 4.2d, -4.2d, 0.1d + 0.2d },
      new[] { 1.5f, -2.5f, 0.1f },
      new[] { new DateTime(2021, 1, 15, 12, 30, 40), new DateTime(2020, 2, 29, 0, 0, 0).AddTicks(1234560) },
      new[] { new DateTimeOffset(new DateTime(2021, 1, 15, 12, 30, 40), TimeSpan.FromMinutes(60)), new DateTimeOffset(2020, 2, 29, 1, 2, 3, TimeSpan.FromHours(-3)).AddTicks(1234560) },
      new[] { TimeSpan.FromMinutes(60), TimeSpan.FromSeconds(1.5), TimeSpan.FromMinutes(-90) },
      new[] { new ConvertibleClass(1), new ConvertibleClass(2), new ConvertibleClass(-3) },
   ]);

   // NULL elements. DISTINCT keeps a single NULL.
   public static readonly IEnumerable<object[]> NullableValues = WithDistinctFlags(
   [
      new int?[] { 1, null, 3 },
      new long?[] { 1L, null, 3L },
      new short?[] { 1, null },
      new byte?[] { 1, null },
      new bool?[] { true, null, false },
      new decimal?[] { 1.5m, null },
      new double?[] { 1.5d, null },
      new float?[] { 1.5f, null },
      new Guid?[] { new Guid("CE5E3D80-13D6-43C3-BB41-6499E2AD6B63"), null },
      new DateTime?[] { new DateTime(2021, 1, 15, 12, 30, 40), null },
      new DateTimeOffset?[] { new DateTimeOffset(new DateTime(2021, 1, 15, 12, 30, 40), TimeSpan.FromMinutes(60)), null },
      new TimeSpan?[] { TimeSpan.FromMinutes(60), null },
   ]);

   // Limits of the MySQL column types: TIME ranges from -838:59:59 to 838:59:59, DATETIME(6) from 1000-01-01 to 9999-12-31 23:59:59.999999.
   public static readonly IEnumerable<object[]> BoundaryValues = WithDistinctFlags(
   [
      new[] { Int32.MinValue, 0, Int32.MaxValue },
      new[] { Int64.MinValue, 0L, Int64.MaxValue },
      new[] { Int16.MinValue, Int16.MaxValue },
      new[] { Byte.MinValue, Byte.MaxValue },
      new[] { Double.MinValue, Double.MaxValue },
      new[] { Decimal.MinValue, Decimal.MaxValue, 0.0000000000000000000000000001m },
      new[] { Guid.Empty, new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF") },
      new[] { new DateTime(1000, 1, 1), new DateTime(9999, 12, 31, 23, 59, 59).AddTicks(9999990) },
      new[] { TimeSpan.FromHours(24), TimeSpan.FromHours(36) + TimeSpan.FromMilliseconds(1.5), TimeSpan.FromHours(-36) },
      new[] { new TimeSpan(838, 59, 59), new TimeSpan(-838, -59, -59) },
   ]);

   public CreateScalarCollectionParameter(MySqlContainerFixture fixture)
      : base(fixture)
   {
      ConfigureMySql = builder => builder.AddCollectionParameterSupport(CollectionParameterJsonSerializerOptions.Default);
   }

   [Theory]
   [MemberData(nameof(Values))]
   public async Task Should_work_with_default_data_types_with_distinct(object values)
   {
      await AssertRoundtripAsync((Array)values, true);
   }

   [Theory]
   [MemberData(nameof(Values))]
   public async Task Should_work_with_default_data_types_without_distinct(object values)
   {
      await AssertRoundtripAsync((Array)values, false);
   }

   [Theory]
   [MemberData(nameof(MultipleValues))]
   public async Task Should_work_with_multiple_values(object values, bool applyDistinct)
   {
      await AssertRoundtripAsync((Array)values, applyDistinct);
   }

   [Theory]
   [MemberData(nameof(NullableValues))]
   public async Task Should_work_with_nullable_values(object values, bool applyDistinct)
   {
      await AssertRoundtripAsync((Array)values, applyDistinct);
   }

   [Theory]
   [MemberData(nameof(BoundaryValues))]
   public async Task Should_work_with_boundary_values(object values, bool applyDistinct)
   {
      await AssertRoundtripAsync((Array)values, applyDistinct);
   }

   [Theory]
   [InlineData("it's")]
   [InlineData("say \"hi\"")]
   [InlineData("back\\slash")]
   [InlineData("C:\\temp\\new\\table")]
   [InlineData("{0}")]
   [InlineData("{1}")]
   [InlineData("{0} and {1} and {{2}}")]
   [InlineData("100% _sure_")]
   [InlineData("[1,2,3]")]
   [InlineData("{\"a\":1}")]
   [InlineData("$.path[*]")]
   [InlineData("'; DROP TABLE TestEntities; --")]
   [InlineData("äöüß 日本語 Ελληνικά")]
   [InlineData("emoji 😀🚀 and surrogate pair 𝄞")]
   [InlineData("line1\nline2\r\nline3\ttab")]
   [InlineData("")]
   [InlineData("  leading and trailing  ")]
   public async Task Should_work_with_special_strings(string value)
   {
      var result = await ActDbContext.CreateScalarCollectionParameter(new[] { value }, applyDistinct: false).ToListAsync();

      result.Should().Equal(value);
   }

   [Fact]
   public async Task Should_work_with_all_special_strings_in_one_collection()
   {
      var values = new[] { "it's", "say \"hi\"", "back\\slash", "{0}", "{1}", "100% _sure_", "äöüß 日本語", "😀", "", "line1\nline2" };

      var result = await ActDbContext.CreateScalarCollectionParameter(values, applyDistinct: true).ToListAsync();

      result.Should().BeEquivalentTo(values);
   }

   [Fact]
   public async Task Should_work_with_strings_exceeding_the_limit_of_a_text_column()
   {
      var value = new string('x', 70_000);

      var result = await ActDbContext.CreateScalarCollectionParameter(new[] { value }, applyDistinct: false).ToListAsync();

      result.Should().ContainSingle().Which.Length.Should().Be(70_000);
   }

   [Fact]
   public async Task Should_work_with_date_only_and_time_only()
   {
      ConfigureModel = builder =>
                       {
                          builder.ConfigureScalarCollectionParameter<DateOnly>();
                          builder.ConfigureScalarCollectionParameter<TimeOnly>();
                       };

      var dates = new[] { new DateOnly(2021, 1, 15), new DateOnly(1000, 1, 1), new DateOnly(9999, 12, 31) };
      var times = new[] { new TimeOnly(12, 30, 40), new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59, 999, 900) };

      (await ActDbContext.CreateScalarCollectionParameter(dates, applyDistinct: false).ToListAsync()).Should().BeEquivalentTo(dates);
      (await ActDbContext.CreateScalarCollectionParameter(times, applyDistinct: true).ToListAsync()).Should().BeEquivalentTo(times);
   }

   [Fact]
   public async Task Should_work_with_enums()
   {
      ConfigureModel = builder =>
                       {
                          builder.ConfigureScalarCollectionParameter<MySqlTestStatus>();
                          builder.ConfigureScalarCollectionParameter<PhaseMembership>();
                       };

      var statuses = new[] { MySqlTestStatus.Published, MySqlTestStatus.Draft };
      var phases = new[] { PhaseMembership.A | PhaseMembership.C, PhaseMembership.None, PhaseMembership.B };

      (await ActDbContext.CreateScalarCollectionParameter(statuses, applyDistinct: false).ToListAsync()).Should().BeEquivalentTo(statuses);
      (await ActDbContext.CreateScalarCollectionParameter(phases, applyDistinct: true).ToListAsync()).Should().BeEquivalentTo(phases);
   }

   // The factory serializes the values with the converter returned by IProperty.GetValueConverter(), i.e. an explicitly configured converter instance.
   // A conversion configured with HasConversion<string>() has no such instance (EF infers it from the provider type), so the CLR value would be sent unconverted.
   [Fact]
   public async Task Should_work_with_enum_converted_to_string_and_filter_a_table()
   {
      ConfigureModel = builder => builder.ConfigureScalarCollectionParameter<MySqlTestStatus>(b => b.Property(e => e.Value).HasConversion(new EnumToStringConverter<MySqlTestStatus>()).HasMaxLength(20));

      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Draft },
                                              new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Published });
      await ArrangeDbContext.SaveChangesAsync();

      var statuses = ActDbContext.CreateScalarCollectionParameter(new[] { MySqlTestStatus.Published }, applyDistinct: true);

      (await statuses.ToListAsync()).Should().Equal(MySqlTestStatus.Published);
      (await ActDbContext.TypedEntities.Where(e => statuses.Contains(e.Status)).Select(e => e.Status).ToListAsync()).Should().Equal(MySqlTestStatus.Published);
   }

   // The usual way to store an enum as string. The column of the parameter is varchar(20), so the JSON must contain "Published" and not the number 1.
   // Currently the number is sent: the factory only applies a converter that is returned by IProperty.GetValueConverter() (see the test above).
   // The round trip still works because MySQL converts 1 to '1' and EF parses the string '1' as enum, but a filter on the string column finds nothing.
   [Fact]
   public async Task Should_work_with_enum_converted_to_string_by_provider_type_and_filter_a_table()
   {
      ConfigureModel = builder => builder.ConfigureScalarCollectionParameter<MySqlTestStatus>(b => b.Property(e => e.Value).HasConversion<string>().HasMaxLength(20));

      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Draft },
                                              new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Published });
      await ArrangeDbContext.SaveChangesAsync();

      var statuses = ActDbContext.CreateScalarCollectionParameter(new[] { MySqlTestStatus.Published }, applyDistinct: true);

      (await statuses.ToListAsync()).Should().Equal(MySqlTestStatus.Published);
      (await ActDbContext.TypedEntities.Where(e => statuses.Contains(e.Status)).Select(e => e.Status).ToListAsync()).Should().Equal(MySqlTestStatus.Published);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_return_empty_result_for_empty_collection(bool applyDistinct)
   {
      var intResult = await ActDbContext.CreateScalarCollectionParameter(Array.Empty<int>(), applyDistinct).ToListAsync();
      var stringResult = await ActDbContext.CreateScalarCollectionParameter(Array.Empty<string>(), applyDistinct).ToListAsync();
      var guidResult = await ActDbContext.CreateScalarCollectionParameter(Array.Empty<Guid>(), applyDistinct).ToListAsync();

      intResult.Should().BeEmpty();
      stringResult.Should().BeEmpty();
      guidResult.Should().BeEmpty();
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_not_find_anything_with_contains_and_empty_collection(bool applyDistinct)
   {
      await ArrangeDbContext.BulkInsertAsync([new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" }]);

      var ids = ActDbContext.CreateScalarCollectionParameter(Array.Empty<Guid>(), applyDistinct);

      (await ActDbContext.TestEntities.Where(e => ids.Contains(e.Id)).ToListAsync()).Should().BeEmpty();
      (await ActDbContext.TestEntities.Where(e => !ids.Contains(e.Id)).ToListAsync()).Should().ContainSingle();
   }

   [Fact]
   public async Task Should_work_with_large_collection()
   {
      var values = Enumerable.Range(1, 20_000).ToList();

      var query = ActDbContext.CreateScalarCollectionParameter(values, applyDistinct: false);

      (await query.CountAsync()).Should().Be(20_000);
      (await query.MinAsync()).Should().Be(1);
      (await query.MaxAsync()).Should().Be(20_000);
      (await query.SumAsync()).Should().Be(200_010_000);
   }

   [Fact]
   public async Task Should_work_with_large_collection_with_distinct()
   {
      var values = Enumerable.Range(0, 20_000).Select(i => i % 100).ToList();

      var result = await ActDbContext.CreateScalarCollectionParameter(values, applyDistinct: true).OrderBy(v => v).ToListAsync();

      result.Should().Equal(Enumerable.Range(0, 100));
   }

   [Fact]
   public async Task Should_work_with_large_collection_of_strings_and_guids()
   {
      var strings = Enumerable.Range(0, 5_000).Select(i => $"value '{i}' \"quoted\" {{{i}}}").ToList();
      var guids = Enumerable.Range(0, 5_000).Select(i => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)).ToList();

      (await ActDbContext.CreateScalarCollectionParameter(strings, applyDistinct: true).CountAsync()).Should().Be(5_000);
      (await ActDbContext.CreateScalarCollectionParameter(guids, applyDistinct: false).CountAsync()).Should().Be(5_000);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_work_with_contains(bool applyDistinct)
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.CreateScalarCollectionParameter([testEntity.Id], applyDistinct);
      var loadedEntities = await ActDbContext.TestEntities
                                             .Where(e => collectionParameter.Contains(e.Id))
                                             .ToListAsync();

      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = _TEST_ENTITY_ID,
                                              Name = "Name",
                                              RequiredName = "RequiredName",
                                              Count = 42
                                           });
   }

   [Fact]
   public async Task Should_work_with_contains_and_not_contains_for_several_entities()
   {
      await SeedTestEntitiesAsync();

      var ids = ActDbContext.CreateScalarCollectionParameter([_TEST_ENTITY_ID, Guid.NewGuid()]);

      var found = await ActDbContext.TestEntities.Where(e => ids.Contains(e.Id)).Select(e => e.RequiredName).ToListAsync();
      var notFound = await ActDbContext.TestEntities.Where(e => !ids.Contains(e.Id)).OrderBy(e => e.RequiredName).Select(e => e.RequiredName).ToListAsync();

      found.Should().Equal("first");
      notFound.Should().Equal("second", "third");
   }

   [Fact]
   public async Task Should_work_with_contains_for_strings()
   {
      await SeedTestEntitiesAsync();

      var names = ActDbContext.CreateScalarCollectionParameter(["second", "third", "does not exist"]);

      var result = await ActDbContext.TestEntities.Where(e => names.Contains(e.RequiredName)).OrderBy(e => e.RequiredName).Select(e => e.RequiredName).ToListAsync();

      result.Should().Equal("second", "third");
   }

   [Fact]
   public async Task Should_work_with_contains_for_nullable_strings_and_nullable_ints()
   {
      await SeedTestEntitiesAsync();

      var names = ActDbContext.CreateScalarCollectionParameter(["Name of first"]);
      var counts = ActDbContext.CreateScalarCollectionParameter<int?>([2, 3]);

      (await ActDbContext.TestEntities.Where(e => names.Contains(e.Name!)).Select(e => e.RequiredName).ToListAsync()).Should().Equal("first");
      (await ActDbContext.TestEntities.Where(e => counts.Contains(e.NullableCount)).OrderBy(e => e.RequiredName).Select(e => e.RequiredName).ToListAsync()).Should().Equal("second", "third");
   }

   [Fact]
   public async Task Should_work_with_contains_and_value_converter()
   {
      await SeedTestEntitiesAsync();

      var convertibleClasses = ActDbContext.CreateScalarCollectionParameter([new ConvertibleClass(10), new ConvertibleClass(30)]);

      var result = await ActDbContext.TestEntities
                                     .Where(e => convertibleClasses.Contains(e.ConvertibleClass!))
                                     .OrderBy(e => e.RequiredName)
                                     .Select(e => e.RequiredName)
                                     .ToListAsync();

      result.Should().Equal("first", "third");
   }

   [Fact]
   public async Task Should_work_with_two_collection_parameters_in_one_query()
   {
      await SeedTestEntitiesAsync();

      var ids = ActDbContext.CreateScalarCollectionParameter([_TEST_ENTITY_ID, _OTHER_TEST_ENTITY_ID]);
      var counts = ActDbContext.CreateScalarCollectionParameter([2, 3, 4]);

      var result = await ActDbContext.TestEntities
                                     .Where(e => ids.Contains(e.Id) && counts.Contains(e.Count))
                                     .Select(e => e.RequiredName)
                                     .ToListAsync();

      result.Should().Equal("second");
   }

   [Fact]
   public async Task Should_work_with_the_same_parameter_used_twice_in_one_query()
   {
      await SeedTestEntitiesAsync();

      var counts = ActDbContext.CreateScalarCollectionParameter([1, 3]);

      var result = await ActDbContext.TestEntities
                                     .Where(e => counts.Contains(e.Count) || counts.Contains(e.Count + 1))
                                     .OrderBy(e => e.RequiredName)
                                     .Select(e => e.RequiredName)
                                     .ToListAsync();

      result.Should().Equal("first", "second", "third");
   }

   [Fact]
   public async Task Should_work_with_joins()
   {
      await SeedTestEntitiesAsync();

      var counts = ActDbContext.CreateScalarCollectionParameter([1, 3, 99]);

      var result = await ActDbContext.TestEntities
                                     .Join(counts, e => e.Count, count => count, (e, count) => new { e.RequiredName, Count = count })
                                     .OrderBy(e => e.Count)
                                     .ToListAsync();

      result.Should().BeEquivalentTo([new { RequiredName = "first", Count = 1 }, new { RequiredName = "third", Count = 3 }], options => options.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_work_with_filter_projection_and_ordering_on_the_parameter()
   {
      var values = ActDbContext.CreateScalarCollectionParameter([5, 1, 4, 2, 3]);

      var result = await values.Where(v => v > 1).Select(v => v * 10).OrderByDescending(v => v).Take(3).ToListAsync();

      result.Should().Equal(50, 40, 30);
   }

   [Fact]
   public async Task Should_work_with_aggregates_on_the_parameter()
   {
      var values = ActDbContext.CreateScalarCollectionParameter([5, 1, 4, 2, 3]);

      (await values.CountAsync()).Should().Be(5);
      (await values.SumAsync()).Should().Be(15);
      (await values.MinAsync()).Should().Be(1);
      (await values.MaxAsync()).Should().Be(5);
      (await values.AnyAsync(v => v == 4)).Should().BeTrue();
      (await values.AnyAsync(v => v == 6)).Should().BeFalse();
   }

   [Fact]
   public async Task Should_work_with_aggregate_on_decimals_of_the_parameter()
   {
      var values = ActDbContext.CreateScalarCollectionParameter([1.25m, 2.5m, 3.125m]);

      (await values.SumAsync()).Should().Be(6.875m);
      (await values.MaxAsync()).Should().Be(3.125m);
   }

   [Fact]
   public async Task Should_work_GroupBy_and_aggregate()
   {
      var testEntity = new TestEntity
                       {
                          Id = _TEST_ENTITY_ID,
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.CreateScalarCollectionParameter([testEntity.Id]);
      var loadedEntities = await ActDbContext.TestEntities
                                             .Where(e => collectionParameter.Contains(e.Id))
                                             .GroupBy(e => e.Id)
                                             .Select(g => new { g.Key, Aggregate = g.Count() })
                                             .ToListAsync();

      loadedEntities.Should().BeEquivalentTo([
                                                new
                                                {
                                                   Key = _TEST_ENTITY_ID,
                                                   Aggregate = 1
                                                },
                                             ]);
   }

   [Fact]
   public async Task Should_work_with_sum_count_and_group_by_on_a_table_filtered_by_the_parameter()
   {
      await ArrangeDbContext.BulkInsertAsync([
                                                new TestEntity { Id = Guid.NewGuid(), Name = "A", RequiredName = "1", Count = 1 },
                                                new TestEntity { Id = Guid.NewGuid(), Name = "A", RequiredName = "2", Count = 2 },
                                                new TestEntity { Id = Guid.NewGuid(), Name = "A", RequiredName = "3", Count = 3 },
                                                new TestEntity { Id = Guid.NewGuid(), Name = "B", RequiredName = "4", Count = 4 },
                                                new TestEntity { Id = Guid.NewGuid(), Name = "B", RequiredName = "5", Count = 5 },
                                                new TestEntity { Id = Guid.NewGuid(), Name = "C", RequiredName = "6", Count = 6 }
                                             ]);

      var counts = ActDbContext.CreateScalarCollectionParameter([2, 3, 4, 5, 99]);

      var result = await ActDbContext.TestEntities
                                     .Where(e => counts.Contains(e.Count))
                                     .GroupBy(e => e.Name)
                                     .Select(g => new { Name = g.Key, Number = g.Count(), Sum = g.Sum(e => e.Count), Max = g.Max(e => e.Count) })
                                     .OrderBy(g => g.Name)
                                     .ToListAsync();

      result.Should().BeEquivalentTo([
                                        new { Name = "A", Number = 2, Sum = 5, Max = 3 },
                                        new { Name = "B", Number = 2, Sum = 9, Max = 5 },
                                     ],
                                     options => options.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_read_the_collection_with_json_table()
   {
      var collectionParameter = ActDbContext.CreateScalarCollectionParameter([1, 2, 3], applyDistinct: true);

      var result = await collectionParameter.OrderBy(v => v).ToListAsync();

      result.Should().Equal(1, 2, 3);
      var command = ExecutedCommands.Should().ContainSingle(c => c.Contains("JSON_TABLE", StringComparison.Ordinal)).Subject;
      command.Should().Contain("SELECT DISTINCT").And.Contain("LIMIT");
   }

   private async Task SeedTestEntitiesAsync()
   {
      await ArrangeDbContext.BulkInsertAsync([
                                                new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name of first", RequiredName = "first", Count = 1, NullableCount = 1, ConvertibleClass = new ConvertibleClass(10) },
                                                new TestEntity { Id = _OTHER_TEST_ENTITY_ID, RequiredName = "second", Count = 2, NullableCount = 2, ConvertibleClass = new ConvertibleClass(20) },
                                                new TestEntity { Id = Guid.NewGuid(), RequiredName = "third", Count = 3, NullableCount = 3, ConvertibleClass = new ConvertibleClass(30) }
                                             ]);
   }

   private Task AssertRoundtripAsync(Array values, bool applyDistinct)
   {
      return (Task)_genericRoundtripTest.MakeGenericMethod(values.GetType().GetElementType()!).Invoke(this, [values, applyDistinct])!;
   }

   private async Task AssertGenericRoundtripAsync<T>(T[] values, bool applyDistinct)
   {
      var result = await ActDbContext.CreateScalarCollectionParameter(values, applyDistinct).ToListAsync();

      result.Should().BeEquivalentTo(values);
   }

   private static IEnumerable<object[]> WithDistinctFlags(IEnumerable<Array> values)
   {
      return values.SelectMany(array => new[] { new object[] { array, true }, new object[] { array, false } });
   }
}
