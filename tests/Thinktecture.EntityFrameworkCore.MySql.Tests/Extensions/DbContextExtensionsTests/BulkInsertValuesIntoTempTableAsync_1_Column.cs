using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// The rows are written with raw ADO commands (multi-row INSERT or MySqlBulkCopy), so the INSERT statements are not visible in ExecutedCommands.
// The data type tests run with both write paths (multi-row INSERT and MySqlBulkCopy), because MySqlBulkCopy converts the values on its own.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertValuesIntoTempTableAsync_1_Column : MySqlIntegrationTestsBase
{
   public BulkInsertValuesIntoTempTableAsync_1_Column(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new List<int> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values);

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo([1, 2]);
   }

   [Fact]
   public async Task Should_insert_int_into_existing_into_empty_temp_table()
   {
      // Arrange
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int>());

      // Act
      await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 3, 4 }, query);

      // Assert
      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo([3, 4]);
   }

   [Fact]
   public async Task Should_insert_int_into_existing_into_non_empty_temp_table()
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

      // 1062: duplicate entry for the primary key
      var exception = await ActDbContext.Awaiting(ctx => ctx.BulkInsertValuesIntoTempTableAsync(values))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);
   }

   [Fact]
   public async Task Should_insert_nullable_int_of_a_keyless_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      var values = new List<int?> { 1, null };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo([1, (int?)null]);
   }

   [Fact]
   public async Task Should_insert_nullable_int_of_entity_with_a_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>(false, typeBuilder =>
                                                                          {
                                                                             typeBuilder.HasKey(t => t.Column1);
                                                                             typeBuilder.Property(t => t.Column1).ValueGeneratedNever();
                                                                          });

      var values = new List<int?> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo([1, 2]);
   }

   [Fact]
   public async Task Should_insert_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string?>(typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired(false));

      var values = new List<string?> { "value1", null };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var tempTable = await query.Query.ToListAsync();
      tempTable.Should().BeEquivalentTo(new[] { "value1", null });
   }

   [Fact]
   public async Task Should_create_pk_by_default_on_string_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(100).IsRequired());

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<string> { "value" }, new MySqlTempTableBulkInsertOptions
                                                                                                                      {
                                                                                                                         TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                         PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced
                                                                                                                      });

      var keys = await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name);
      keys.Should().Equal(nameof(TempTable<string>.Column1));
   }

   [Fact]
   public async Task Should_throw_when_trying_to_create_pk_on_nullable_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      // 1171: all parts of a PRIMARY KEY must be NOT NULL
      var exception = await ActDbContext.Awaiting(ctx => ctx.BulkInsertValuesIntoTempTableAsync(new List<int?> { 1 }, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced }))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1171);
   }

   [Fact]
   public async Task Should_throw_when_trying_to_create_pk_on_string_column_without_max_length()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired());

      // 1170: BLOB/TEXT column used in key specification without a key length
      var exception = await ActDbContext.Awaiting(ctx => ctx.BulkInsertValuesIntoTempTableAsync(new List<string> { "value" }, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced }))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1170);
   }

   [Fact]
   public async Task Should_create_pk_by_default()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new MySqlTempTableBulkInsertOptions
                                                                                                             {
                                                                                                                TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced
                                                                                                             });

      var keys = await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name);
      keys.Should().Equal(nameof(TempTable<int>.Column1));
   }

   [Fact]
   public async Task Should_not_create_pk_if_entity_is_keyless_and_provider_is_AccordingToEntityTypeConfiguration()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new MySqlTempTableBulkInsertOptions
                                                                                                             {
                                                                                                                TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                                PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration
                                                                                                             });

      var keys = await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name);
      keys.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_not_create_pk_if_specified_in_options()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new MySqlTempTableBulkInsertOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None });

      var keys = await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name);
      keys.Should().BeEmpty();
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
      // The query is translated as for other databases. MySQL rejects it on execution, see Should_fail_with_error_1137_when_joining_temp_table_with_itself.
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { Guid.Empty });

      var joinQuery = tempTable.Query.LeftJoin(tempTable.Query, e => e, e => e, (e1, e2) => new { Left = e1, Right = e2 });

      joinQuery.ToQueryString().Should().Be("""
                                            SELECT `#`.`Column1` AS `Left`, `#0`.`Column1` AS `Right`
                                            FROM `#TempTable<Guid>_1` AS `#`
                                            LEFT JOIN `#TempTable<Guid>_1` AS `#0` ON `#`.`Column1` = `#0`.`Column1`
                                            """.WithEnvironmentLineBreaks());
   }

   [Fact]
   public async Task Should_fail_with_error_1137_when_joining_temp_table_with_itself()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { Guid.Empty });

      // 1137: MySQL can't reopen a temporary table within one statement
      var exception = await tempTable.Query.LeftJoin(tempTable.Query, e => e, e => e, (e1, e2) => new { Left = e1, Right = e2 })
                                     .Awaiting(q => q.ToListAsync())
                                     .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1137);
   }

   [Fact]
   public async Task Should_join_two_temp_tables_with_same_values()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      // The workaround for error 1137: every reference needs its own temp table.
      await using var first = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { 1, 2, 3 });
      await using var second = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { 2, 3, 4 });

      first.Name.Should().NotBe(second.Name);

      var joined = await first.Query.Join(second.Query, e => e, e => e, (e1, e2) => e1).OrderBy(e => e).ToListAsync();
      joined.Should().Equal(2, 3);
   }

   [Fact]
   public async Task Should_filter_real_table_with_values_of_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var firstId = new Guid("6EB48130-454B-46BC-9FEE-CDF411F69807");
      var secondId = new Guid("17A2014C-F7B2-40E8-82F4-42E754DCAA2D");
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = firstId, RequiredName = "First" },
                                             new TestEntity { Id = secondId, RequiredName = "Second" });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { secondId, Guid.NewGuid() });

      var names = await ActDbContext.TestEntities
                                    .Where(e => tempTable.Query.Contains(e.Id))
                                    .Select(e => e.RequiredName)
                                    .ToListAsync();

      names.Should().Equal("Second");
   }

   [Fact]
   public async Task Should_return_number_of_inserted_rows()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new List<int> { 1, 2 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values);

      query.NumberOfInsertedRows.Should().Be(2);
   }

   // The tests below cover behavior specific to MySQL: write paths (multi-row INSERT and MySqlBulkCopy) and the value ranges of the MySQL column types.

   [Fact]
   public async Task Should_insert_empty_values()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int>());

      query.NumberOfInsertedRows.Should().Be(0);
      (await query.Query.ToListAsync()).Should().BeEmpty();
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_many_values(bool useBulkCopy)
   {
      var values = Enumerable.Range(0, 5001).ToList();

      await AssertRoundTripAsync(useBulkCopy, values);
   }

   [Theory]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(5)]
   [InlineData(100)]
   public async Task Should_insert_values_with_batch_size(int batchSize)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var values = new List<int> { 1, 2, 3, 4, 5 };
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new MySqlTempTableBulkInsertOptions { BatchSize = batchSize });

      query.NumberOfInsertedRows.Should().Be(5);
      (await query.Query.OrderBy(v => v).ToListAsync()).Should().Equal(1, 2, 3, 4, 5);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_insert_values_into_existing_temp_table(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      var options = CreateOptions(useBulkCopy);
      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1, 2 }, options);

      await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 3, 4 }, query, options);

      (await query.Query.OrderBy(v => v).ToListAsync()).Should().Equal(1, 2, 3, 4);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_int(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { Int32.MinValue, -1, 0, 1, Int32.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_int(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new int?[] { Int32.MinValue, null, 0, null, Int32.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_long(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { Int64.MinValue, -1L, 0L, 1L, Int64.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_long(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new long?[] { Int64.MinValue, null, Int64.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_short(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { Int16.MinValue, (short)-1, (short)0, (short)1, Int16.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_short(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new short?[] { Int16.MinValue, null, Int16.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_byte(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new byte[] { Byte.MinValue, 1, 127, 128, Byte.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_byte(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new byte?[] { Byte.MinValue, null, Byte.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_bool(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { true, false, true });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_bool(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new bool?[] { true, null, false });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_string(bool useBulkCopy)
   {
      string[] values =
      [
         "",
         "value",
         "  padded  ",
         "O'Brien",
         "say \"hi\"",
         "back\\slash",
         "trailing backslash\\",
         "\\N",
         "NULL",
         "line1\nline2",
         "line1\r\nline2",
         "tab\tseparated",
         "null\0character",
         "`backtick`",
         "100%_",
         "'; DROP TABLE TestEntities; --",
         "umlauts \u00E4\u00F6\u00FC\u00DF, \u65E5\u672C\u8A9E, emoji \U0001F600"
      ];

      await AssertRoundTripAsync(useBulkCopy, values);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_string_longer_than_64_kilobytes(bool useBulkCopy)
   {
      // longtext
      var value = String.Concat(Enumerable.Repeat("0123456789 \u00E4\u00F6\u00FC ", 8000));
      value.Length.Should().BeGreaterThan(64 * 1024);

      await AssertRoundTripAsync(useBulkCopy, new[] { value });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_string(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new string?[] { "value", null, "", null },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired(false));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_string_with_max_length(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[] { "", "short", new string('x', 50) },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(50));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_for_string_longer_than_max_length(bool useBulkCopy)
   {
      // 1406: data too long for the column (strict mode)
      await AssertInsertFailsAsync(useBulkCopy,
                                   new[] { new string('x', 51) },
                                   1406,
                                   typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(50));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_guid(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    Guid.Empty,
                                    new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                                    new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_guid(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new Guid?[] { new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"), null, Guid.Empty });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_decimal(bool useBulkCopy)
   {
      // decimal(38,18)
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    0m,
                                    1.25m,
                                    -1.25m,
                                    0.000000000000000001m,
                                    12345678901234567.89m,
                                    12345678901234567890.123456789012345678m,
                                    -12345678901234567890.123456789012345678m
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_decimal(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new decimal?[] { 1.25m, null, -0.5m });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_decimal_with_explicit_precision(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[] { 0m, 1.25m, -999999999999999.99999m },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasPrecision(20, 5));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_for_decimal_beyond_range_of_the_column(bool useBulkCopy)
   {
      // decimal(38,18) has 20 integer digits only, decimal.MaxValue has 29. 1264: out of range value for column
      await AssertInsertFailsAsync(useBulkCopy, new[] { Decimal.MaxValue }, 1264);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_double(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    0d,
                                    1.5,
                                    -1.5,
                                    0.1,
                                    1d / 3,
                                    Math.PI,
                                    1.7976931348623157E+308,
                                    -1.7976931348623157E+308,
                                    2.2250738585072014E-308
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_double(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new double?[] { 1.5, null, -0.25 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_float(bool useBulkCopy)
   {
      // MySQL returns a FLOAT with 6 significant digits, so these values have at most 6 digits.
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    0f,
                                    1.5f,
                                    -1.5f,
                                    0.1f,
                                    123456f,
                                    1E+38f,
                                    -1E+38f,
                                    1.17549E-38f
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_return_float_with_six_significant_digits(bool useBulkCopy)
   {
      // MySQL sends a FLOAT as text with 6 significant digits, i.e. the value is stored precisely but read with a lower precision.
      ConfigureModel = builder => builder.ConfigureTempTable<float>();

      var options = CreateOptions(useBulkCopy);

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { 1f / 3, MathF.PI }, options);

      (await query.Query.ToListAsync()).Should().BeEquivalentTo([0.333333f, 3.14159f]);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_float(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new float?[] { 1.5f, null, -0.25f });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_for_float_max_value(bool useBulkCopy)
   {
      // The text of float.MaxValue (3.4028235E+38) is slightly above the maximum of the MySQL type FLOAT. 1264: out of range value for column
      await AssertInsertFailsAsync(useBulkCopy, new[] { Single.MaxValue }, 1264);
   }

   [Theory]
   [InlineData(false, Double.NaN)]
   [InlineData(false, Double.PositiveInfinity)]
   [InlineData(false, Double.NegativeInfinity)]
   [InlineData(true, Double.NaN)]
   [InlineData(true, Double.PositiveInfinity)]
   [InlineData(true, Double.NegativeInfinity)]
   public async Task Should_throw_for_not_a_number_and_infinity(bool useBulkCopy, double value)
   {
      // MySQL cannot store NaN or Infinity. The driver writes them as identifiers. 1054: unknown column
      await AssertInsertFailsAsync(useBulkCopy, new[] { value }, 1054);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_double_epsilon(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { Double.Epsilon, -Double.Epsilon });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_date_time(bool useBulkCopy)
   {
      // datetime(6): microseconds are kept, the supported range is 1000-01-01 to 9999-12-31
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    new DateTime(2020, 1, 2, 3, 4, 5),
                                    new DateTime(2020, 1, 2, 3, 4, 5).AddTicks(1234560),
                                    new DateTime(2020, 1, 2, 3, 4, 5).AddTicks(10),
                                    new DateTime(1000, 1, 1),
                                    new DateTime(9999, 12, 31, 23, 59, 59)
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_date_time(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new DateTime?[] { new DateTime(2020, 1, 2, 3, 4, 5), null, new DateTime(2021, 12, 31, 23, 59, 59, 999) });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_truncate_date_time_to_microseconds(bool useBulkCopy)
   {
      // datetime(6) has a precision of 1 microsecond (10 ticks); the remainder is truncated, not rounded.
      ConfigureModel = builder => builder.ConfigureTempTable<DateTime>();

      var options = CreateOptions(useBulkCopy);
      var values = new[]
                   {
                      new DateTime(2020, 1, 2, 3, 4, 5).AddTicks(1234567),
                      new DateTime(2020, 1, 2, 3, 4, 6).AddTicks(9),
                      DateTime.MaxValue
                   };

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, options);

      (await query.Query.ToListAsync()).Should().BeEquivalentTo([
                                                                   new DateTime(2020, 1, 2, 3, 4, 5).AddTicks(1234560),
                                                                   new DateTime(2020, 1, 2, 3, 4, 6),
                                                                   DateTime.MaxValue.AddTicks(-9)
                                                                ]);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_date_time_min_value_although_it_is_outside_of_the_documented_range(bool useBulkCopy)
   {
      // The documented range of DATETIME starts at 1000-01-01, but MySQL accepts the earlier dates.
      await AssertRoundTripAsync(useBulkCopy, new[] { DateTime.MinValue, new DateTime(1, 1, 2) });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_date_time_offset(bool useBulkCopy)
   {
      // datetime(6) has no offset, i.e. the value is stored as UTC.
      var values = new[]
                   {
                      new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
                      new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234560)
                   };

      await AssertRoundTripAsync(useBulkCopy, values);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_store_date_time_offset_with_offset_as_utc(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<DateTimeOffset>();

      var options = CreateOptions(useBulkCopy);
      var value = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { value }, options);

      var loaded = await query.Query.SingleAsync();
      loaded.UtcDateTime.Should().Be(value.UtcDateTime);
      loaded.Offset.Should().Be(TimeSpan.Zero);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_date_time_offset(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new DateTimeOffset?[] { new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), null });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_time_span(bool useBulkCopy)
   {
      // time(6): from -838:59:59 to 838:59:59, i.e. a TimeSpan can exceed 24 hours
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    TimeSpan.Zero,
                                    new TimeSpan(1, 2, 3),
                                    new TimeSpan(1, 2, 3, 4, 5),
                                    TimeSpan.FromHours(24),
                                    TimeSpan.FromHours(100),
                                    TimeSpan.FromHours(-100),
                                    TimeSpan.FromTicks(10),
                                    new TimeSpan(838, 59, 59),
                                    new TimeSpan(-838, -59, -59)
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_truncate_time_span_to_microseconds(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<TimeSpan>();

      var options = CreateOptions(useBulkCopy);

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { TimeSpan.FromTicks(7), TimeSpan.FromTicks(19) }, options);

      (await query.Query.ToListAsync()).Should().BeEquivalentTo([TimeSpan.Zero, TimeSpan.FromTicks(10)]);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_for_time_span_beyond_range_of_the_column(bool useBulkCopy)
   {
      // time(6) ends at 838:59:59. 1525: incorrect TIME value
      await AssertInsertFailsAsync(useBulkCopy, new[] { TimeSpan.FromHours(839) }, 1525);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_throw_for_time_span_below_range_of_the_column(bool useBulkCopy)
   {
      await AssertInsertFailsAsync(useBulkCopy, new[] { TimeSpan.FromHours(-839) }, 1525);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_time_span(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new TimeSpan?[] { TimeSpan.FromHours(25), null, TimeSpan.FromMinutes(-5) });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_date_only(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    new DateOnly(2020, 1, 2),
                                    new DateOnly(1000, 1, 1),
                                    new DateOnly(9999, 12, 31)
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_date_only_min_and_max_value(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { DateOnly.MinValue, DateOnly.MaxValue });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_date_only(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new DateOnly?[] { new DateOnly(2020, 1, 2), null });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_time_only(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[]
                                 {
                                    TimeOnly.MinValue,
                                    new TimeOnly(13, 14, 15),
                                    new TimeOnly(1, 2, 3, 456),
                                    new TimeOnly(23, 59, 59)
                                 });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_truncate_time_only_to_microseconds(bool useBulkCopy)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<TimeOnly>();

      var options = CreateOptions(useBulkCopy);

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { TimeOnly.MaxValue }, options);

      (await query.Query.ToListAsync()).Should().BeEquivalentTo([new TimeOnly(TimeOnly.MaxValue.Ticks - 9)]);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_time_only(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new TimeOnly?[] { new TimeOnly(13, 14, 15), null });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_byte_array(bool useBulkCopy)
   {
      var largeValue = new byte[100_000];

      for (var i = 0; i < largeValue.Length; i++)
      {
         largeValue[i] = (byte)(i * 31 % 256);
      }

      await AssertRoundTripAsync(useBulkCopy,
                                 new byte[][]
                                 {
                                    [],
                                    [0],
                                    [0, 1, 2, 254, 255],
                                    Encoding.UTF8.GetBytes("O'Brien \\N \"quoted\" \r\n"),
                                    largeValue
                                 },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired());
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_byte_array(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new byte[]?[] { [1, 2, 3], null, [] },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired(false));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_enum(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new[] { PhaseMembership.None, PhaseMembership.A, PhaseMembership.A | PhaseMembership.C, PhaseMembership.A | PhaseMembership.B | PhaseMembership.C });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_enum_with_string_conversion(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[] { PhaseMembership.None, PhaseMembership.A, PhaseMembership.B },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasConversion<string>().HasMaxLength(50));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_enum(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy, new PhaseMembership?[] { PhaseMembership.A, null, PhaseMembership.C });
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_value_with_value_converter(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[] { new ConvertibleClass(1), new ConvertibleClass(Int32.MaxValue), new ConvertibleClass(-5) },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasConversion(c => c.Key, k => new ConvertibleClass(k)));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_nullable_value_with_value_converter(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new ConvertibleClass?[] { new ConvertibleClass(1), null },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasConversion(c => c!.Key, k => new ConvertibleClass(k)).IsRequired(false));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_trip_value_with_string_value_converter(bool useBulkCopy)
   {
      await AssertRoundTripAsync(useBulkCopy,
                                 new[] { new ConvertibleClass(1), new ConvertibleClass(42) },
                                 typeBuilder => typeBuilder.Property(t => t.Column1).HasConversion(c => c.Key.ToString(), k => new ConvertibleClass(Int32.Parse(k))).HasMaxLength(20));
   }

   // MySqlBulkCopy uses LOAD DATA LOCAL INFILE, which the connection has to allow explicitly (the server is started with --local-infile=1).
   // This must happen before the first use of the connection of ActDbContext.
   private MySqlTempTableBulkInsertOptions CreateOptions(bool useBulkCopy)
   {
      if (useBulkCopy)
      {
         var connectionString = new MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = true };
         ActDbContext.Database.SetConnectionString(connectionString.ConnectionString);
      }

      return new MySqlTempTableBulkInsertOptions { UseBulkCopy = useBulkCopy };
   }

   private async Task AssertInsertFailsAsync<T>(
      bool useBulkCopy,
      IReadOnlyList<T> values,
      int mySqlErrorNumber,
      Action<EntityTypeBuilder<TempTable<T>>>? configure = null)
   {
      ConfigureModel = builder => builder.ConfigureTempTable(configure);

      var options = CreateOptions(useBulkCopy);
      var act = () => ActDbContext.BulkInsertValuesIntoTempTableAsync(values, options);

      if (useBulkCopy)
      {
         // LOAD DATA LOCAL implies IGNORE, i.e. MySQL clips or truncates the value and reports a warning, which the executor turns into an exception.
         await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MySqlBulkCopy inserted*");
      }
      else
      {
         var exception = await act.Should().ThrowAsync<MySqlException>();
         exception.Which.Number.Should().Be(mySqlErrorNumber);
      }

      // The temp table is dropped and the connection is released if the insert fails.
      ActDbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
   }

   private async Task AssertRoundTripAsync<T>(
      bool useBulkCopy,
      IReadOnlyList<T> values,
      Action<EntityTypeBuilder<TempTable<T>>>? configure = null)
   {
      ConfigureModel = builder => builder.ConfigureTempTable(configure);

      var options = CreateOptions(useBulkCopy);

      await using var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, options);

      query.NumberOfInsertedRows.Should().Be(values.Count);

      var loadedValues = await query.Query.ToListAsync();
      loadedValues.Should().BeEquivalentTo(values);
   }
}
