using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.Internal;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;

// A temp table exists in the session of the connection that created it, so the structure is read through the connection of ActDbContext.
// MySQL has no INFORMATION_SCHEMA entry for temp tables, the tests use SHOW FULL COLUMNS / SHOW KEYS / SHOW CREATE TABLE.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class CreateTempTableAsync : MySqlIntegrationTestsBase
{
   private readonly TempTableCreationOptions _optionsWithNonUniqueName;
   private readonly TempTableCreationOptions _options;

   private MySqlTempTableCreator SUT => field ??= (MySqlTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreateTempTableAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
      _optionsWithNonUniqueName = new TempTableCreationOptions { TableNameProvider = DefaultTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };
      _options = new TempTableCreationOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };
   }

   [Fact]
   public async Task Should_create_temp_table_for_keyless_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      tempTable.Name.Should().Be("#CustomTempTable");

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_execute_create_temporary_table_statement()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var statement = GetTempTableStatements().Single();
      statement.Should().StartWith("CREATE TEMPORARY TABLE `#CustomTempTable` (");
      statement.Should().Contain("`Column1` int NOT NULL");
      statement.Should().Contain("`Column2` longtext NULL");
      statement.Should().NotContain("PRIMARY KEY");
   }

   [Fact]
   public async Task Should_delete_temp_table_on_dispose_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using var connection = await MySqlTempTableInspector.KeepConnectionOpenAsync(ActDbContext);

      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTable.Name)).Should().BeTrue();

      tempTable.Dispose();

      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTable.Name)).Should().BeFalse();
   }

   [Fact]
   public async Task Should_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using var connection = await MySqlTempTableInspector.KeepConnectionOpenAsync(ActDbContext);

      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await tempTable.DisposeAsync();

      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTable.Name)).Should().BeFalse();
   }

   [Fact]
   public async Task Should_use_existing_transaction_in_disposeAsync()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using var tx = await ActDbContext.Database.BeginTransactionAsync();

      string tempTableName;

      await using (var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
      {
         tempTableName = tempTable.Name;
      }

      // The connection is kept open by the transaction. DROP TEMPORARY TABLE would fail if it doesn't use the transaction of the connection.
      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTableName)).Should().BeFalse();

      await tx.CommitAsync();
   }

   [Fact]
   public async Task Should_not_delete_temp_table_on_dispose_if_DropTableOnDispose_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = false;

      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      try
      {
         tempTable.Dispose();

         // The connection stays open, otherwise the session and with it the temp table would be gone.
         ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);

         var columns = await GetColumnsAsync(tempTable);
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
      }
      finally
      {
         await DropAndReleaseAsync(tempTable);
      }
   }

   [Fact]
   public async Task Should_not_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = false;

      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      try
      {
         await tempTable.DisposeAsync();

         ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);

         var columns = await GetColumnsAsync(tempTable);
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
      }
      finally
      {
         await DropAndReleaseAsync(tempTable);
      }
   }

   [Fact]
   public async Task Should_dispose_temp_table_reference_multiple_times()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _options);

      await tempTable.DisposeAsync();
      await tempTable.DisposeAsync();
      tempTable.Dispose();

      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // The name is available again, i.e. it has been released once only.
      await using var other = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _options);
      other.Name.Should().Be(tempTable.Name);
   }

   [Fact]
   public async Task Should_create_temp_table_with_provided_column_only()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(1);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
   }

   [Fact]
   public async Task Should_create_pk_if_options_flag_is_set()
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;

      ConfigureModel = builder => builder.ConfigureTempTable<int, string>(typeBuilder => typeBuilder.Property(s => s.Column2).IsRequired().HasMaxLength(100));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "int", false);
      ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "varchar(100)", false);

      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name))
         .Should().Equal(nameof(TempTable<int, string>.Column1), nameof(TempTable<int, string>.Column2));
      GetTempTableStatements().Single().Should().Contain("PRIMARY KEY (`Column1`, `Column2`)");
   }

   [Fact]
   public async Task Should_throw_if_some_pk_columns_are_missing()
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration;
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, typeBuilder =>
                                                                                           {
                                                                                              typeBuilder.Property(s => s.Column2).HasMaxLength(100);
                                                                                              typeBuilder.HasKey(s => new { s.Column1, s.Column2 });
                                                                                           });

      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<ArgumentException>().WithMessage("""
                                                                     Cannot create PRIMARY KEY because not all key columns are part of the temp table.
                                                                     You may use other key properties providers like 'IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration' instead of 'IPrimaryKeyPropertiesProvider.EntityTypeConfiguration' to get different behaviors.
                                                                     Missing columns: Column2.
                                                                     """);

      // Neither a table nor an open connection is left behind.
      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_not_throw_if_some_pk_columns_are_missing_and_provider_is_Adaptive(bool forced)
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = forced ? IPrimaryKeyPropertiesProvider.AdaptiveForced : IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration;
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, typeBuilder =>
                                                                                           {
                                                                                              typeBuilder.Property(s => s.Column2).HasMaxLength(100);
                                                                                              typeBuilder.HasKey(s => new { s.Column1, s.Column2 });
                                                                                           });

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().Equal(nameof(CustomTempTable.Column1));
   }

   [Fact]
   public async Task Should_create_pk_of_entity_type_configuration_by_default()
   {
      var options = new TempTableCreationOptions();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntity>(), options);

      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().Equal(nameof(TestEntity.Id));
   }

   [Fact]
   public async Task Should_create_pk_with_provided_properties()
   {
      _options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.From<TestEntity>(e => new { e.Count, e.Id });

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntity>(), _options);

      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().Equal(nameof(TestEntity.Count), nameof(TestEntity.Id));
   }

   [Fact]
   public async Task Should_throw_when_trying_to_create_pk_on_nullable_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      _options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;

      // 1171: all parts of a PRIMARY KEY must be NOT NULL
      var exception = await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<TempTable<int?>>(), _options))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1171);

      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_throw_when_trying_to_create_pk_on_longtext_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired());

      _options.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;

      // 1170: BLOB/TEXT column used in key specification without a key length
      var exception = await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<TempTable<string>>(), _options))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1170);
   }

   [Fact]
   public async Task Should_create_temp_table_with_int()
   {
      await AssertColumnAsync<int>("int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_nullable_int()
   {
      await AssertColumnAsync<int?>("int", true);
   }

   [Fact]
   public async Task Should_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>(typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired());

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<int?>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_long()
   {
      await AssertColumnAsync<long>("bigint", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_short()
   {
      await AssertColumnAsync<short>("smallint", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_byte()
   {
      await AssertColumnAsync<byte>("tinyint unsigned", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string()
   {
      await AssertColumnAsync<string>("longtext", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_max_length()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(50));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<string>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "varchar(50)", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_custom_collation()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(b => b.Property(e => e.Column2).UseCollation("utf8mb4_bin"));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true, collation: "utf8mb4_bin");
      GetTempTableStatements().Single().Should().Contain("`Column2` longtext COLLATE utf8mb4_bin NULL");
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_custom_charset_and_collation()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(b => b.Property(e => e.Column2).HasMaxLength(50).HasCharSet("latin1").UseCollation("latin1_general_cs"));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "varchar(50)", true, collation: "latin1_general_cs");
      GetTempTableStatements().Single().Should().Contain("`Column2` varchar(50) CHARACTER SET latin1 COLLATE latin1_general_cs NULL");
   }

   [Fact]
   public async Task Should_create_temp_table_with_enum_column_with_charset()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<EnumTempTable>(b =>
                                                                                  {
                                                                                     b.Property(e => e.Status).HasColumnType("enum('Draft','Published')").HasCharSet("utf8mb4");
                                                                                  });

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<EnumTempTable>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(EnumTempTable.Status), "enum('Draft','Published')", true);
      columns[0].Collation.Should().StartWith("utf8mb4_");
      GetTempTableStatements().Single().Should().Contain("`Status` enum('Draft','Published') CHARACTER SET utf8mb4 NULL");
   }

   [Fact]
   public async Task Should_create_temp_table_with_bool()
   {
      await AssertColumnAsync<bool>("tinyint(1)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_double()
   {
      await AssertColumnAsync<double>("double", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_float()
   {
      await AssertColumnAsync<float>("float", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_decimal()
   {
      await AssertColumnAsync<decimal>("decimal(38,18)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<decimal>(typeBuilder => typeBuilder.Property(t => t.Column1).HasPrecision(20, 5));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<decimal>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal(20,5)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_guid()
   {
      await AssertColumnAsync<Guid>("char(36)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_date_time()
   {
      await AssertColumnAsync<DateTime>("datetime(6)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_date_time_offset()
   {
      await AssertColumnAsync<DateTimeOffset>("datetime(6)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_date_only()
   {
      await AssertColumnAsync<DateOnly>("date", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_time_only()
   {
      await AssertColumnAsync<TimeOnly>("time(6)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_time_span()
   {
      await AssertColumnAsync<TimeSpan>("time(6)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_byte_array()
   {
      await AssertColumnAsync<byte[]>("longblob", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_enum()
   {
      await AssertColumnAsync<PhaseMembership>("smallint", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_2_columns()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<int, string>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "int", false);
      ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "longtext", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_one_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<int>>(), _options);

      (await GetColumnsAsync(tempTable)).Should().HaveCount(1);
   }

   [Fact]
   public async Task Should_create_temp_table_without_primary_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<int>>(), _options);

      (await MySqlTempTableInspector.GetPrimaryKeyColumnsAsync(ActDbContext, tempTable.Name)).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_create_temp_table_with_converter_and_default_value()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<ConvertibleClass>(typeBuilder => typeBuilder.Property(t => t.Column1)
                                                                                                         .HasConversion(c => c.Key, k => new ConvertibleClass(k))
                                                                                                         .HasDefaultValue(new ConvertibleClass(1)));

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<ConvertibleClass>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      ValidateColumn(columns[0], nameof(TempTable<ConvertibleClass>.Column1), "int", true, defaultValue: "1");
   }

   [Fact]
   public async Task Should_create_temp_table_for_entityType()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntity>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(9);

      ValidateColumn(columns[0], nameof(TestEntity.ConvertibleClass), "int", true);
      ValidateColumn(columns[1], nameof(TestEntity.Count), "int", false);
      ValidateColumn(columns[2], nameof(TestEntity.Id), "char(36)", false);
      ValidateColumn(columns[3], nameof(TestEntity.Name), "longtext", true);
      ValidateColumn(columns[4], nameof(TestEntity.NullableCount), "int", true);
      ValidateColumn(columns[5], nameof(TestEntity.ParentId), "char(36)", true);
      ValidateColumn(columns[6], nameof(TestEntity.PropertyWithBackingField), "int", false);
      ValidateColumn(columns[7], nameof(TestEntity.RequiredName), "longtext", false);
      ValidateColumn(columns[8], "_privateField", "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_for_keyless_entityType()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<KeylessTestEntity>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(1);
      ValidateColumn(columns[0], nameof(KeylessTestEntity.IntColumn), "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_properties_of_base_class()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithBaseClass>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(2);
      ValidateColumn(columns[0], nameof(TestEntityWithBaseClass.Id), "char(36)", false);
      ValidateColumn(columns[1], nameof(TestEntityWithBaseClass.Name), "longtext", false);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_shadow_properties()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithShadowProperties>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(4);
      ValidateColumn(columns[0], nameof(TestEntityWithShadowProperties.Id), "char(36)", false);
      ValidateColumn(columns[1], nameof(TestEntityWithShadowProperties.Name), "longtext", true);
      ValidateColumn(columns[2], "ShadowIntProperty", "int", true);
      ValidateColumn(columns[3], "ShadowStringProperty", "varchar(50)", true);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_different_column_names()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithDifferentColumnNames>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(3);
      ValidateColumn(columns[0], "display_name", "longtext", true);
      ValidateColumn(columns[1], "entity_id", "char(36)", false);
      ValidateColumn(columns[2], "item_count", "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_flags_enum()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithFlags>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(3);
      ValidateColumn(columns[0], nameof(TestEntityWithFlags.GroupId), "int", false);
      ValidateColumn(columns[1], nameof(TestEntityWithFlags.Id), "char(36)", false);
      ValidateColumn(columns[2], nameof(TestEntityWithFlags.Phase), "smallint", false);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_json_column_without_charset()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithJsonColumns>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(2);
      ValidateColumn(columns[0], nameof(TestEntityWithJsonColumns.Id), "char(36)", false);
      ValidateColumn(columns[1], nameof(TestEntityWithJsonColumns.JsonColumn), "json", true);
      columns[1].Collation.Should().BeNull();
   }

   [Fact]
   public async Task Should_create_temp_table_with_sql_default_values()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithSqlDefaultValues>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(5);

      // MySQL requires an expression default like UUID() to be enclosed in parentheses.
      ValidateColumn(columns[0], nameof(TestEntityWithSqlDefaultValues.Id), "char(36)", false);
      columns[0].Default.Should().BeEquivalentTo("(uuid())");
      ValidateColumn(columns[1], nameof(TestEntityWithSqlDefaultValues.Int), "int", false, defaultValue: "1");
      ValidateColumn(columns[2], nameof(TestEntityWithSqlDefaultValues.NullableInt), "int", true, defaultValue: "2");
      ValidateColumn(columns[3], nameof(TestEntityWithSqlDefaultValues.NullableString), "varchar(100)", true, defaultValue: "4");
      ValidateColumn(columns[4], nameof(TestEntityWithSqlDefaultValues.String), "varchar(100)", false, defaultValue: "3");
   }

   [Fact]
   public async Task Should_use_sql_default_values_for_columns_that_are_not_inserted()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithSqlDefaultValues>(), _options);

      await ActDbContext.BulkInsertIntoTempTableAsync([new TestEntityWithSqlDefaultValues { Int = 42 }], tempTable, e => e.Int);

      var loadedEntity = await ActDbContext.TestEntitiesWithDefaultValues.FromTempTable(new TempTableInfo(tempTable.Name)).AsNoTracking().SingleAsync();
      loadedEntity.Id.Should().NotBeEmpty();
      loadedEntity.Int.Should().Be(42);
      loadedEntity.NullableInt.Should().Be(2);
      loadedEntity.String.Should().Be("3");
      loadedEntity.NullableString.Should().Be("4");
   }

   [Fact]
   public async Task Should_create_temp_table_with_dotnet_default_values()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithDotnetDefaultValues>(), _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(5);

      ValidateColumn(columns[0], nameof(TestEntityWithDotnetDefaultValues.Id), "char(36)", false, defaultValue: "0b151271-79bb-4f6c-b85f-e8f61300ff1b");
      ValidateColumn(columns[1], nameof(TestEntityWithDotnetDefaultValues.Int), "int", false, defaultValue: "1");
      ValidateColumn(columns[2], nameof(TestEntityWithDotnetDefaultValues.NullableInt), "int", true, defaultValue: "2");
      ValidateColumn(columns[3], nameof(TestEntityWithDotnetDefaultValues.NullableString), "varchar(100)", true, defaultValue: "4");
      ValidateColumn(columns[4], nameof(TestEntityWithDotnetDefaultValues.String), "varchar(100)", false, defaultValue: "3");
   }

   [Fact]
   public async Task Should_use_dotnet_default_values_for_columns_that_are_not_inserted()
   {
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithDotnetDefaultValues>(), _options);

      await ActDbContext.BulkInsertIntoTempTableAsync([new TestEntityWithDotnetDefaultValues()], tempTable, e => e.NullableString);

      var loadedEntity = await ActDbContext.TestEntitiesWithDotnetDefaultValues.FromTempTable(new TempTableInfo(tempTable.Name)).AsNoTracking().SingleAsync();
      loadedEntity.Id.Should().Be(new Guid("0B151271-79BB-4F6C-B85F-E8F61300FF1B"));
      loadedEntity.Int.Should().Be(1);
      loadedEntity.NullableInt.Should().Be(2);
      loadedEntity.String.Should().Be("3");
      loadedEntity.NullableString.Should().BeNull();
   }

   [Fact]
   public async Task Should_create_temp_table_with_auto_increment_if_it_is_primary_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<AutoIncrementTempTable>(false, b => b.HasKey(e => e.Id));

      var options = new TempTableCreationOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration };

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<AutoIncrementTempTable>(), options);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(AutoIncrementTempTable.Id), "int", false);
      columns[0].Key.Should().Be("PRI");
      columns[0].Extra.Should().Contain("auto_increment");
      ValidateColumn(columns[1], nameof(AutoIncrementTempTable.Name), "longtext", true);
   }

   [Fact]
   public async Task Should_generate_values_for_auto_increment_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<AutoIncrementTempTable>(false, b => b.HasKey(e => e.Id));

      var options = new TempTableCreationOptions { PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration };

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<AutoIncrementTempTable>(), options);

      await ActDbContext.BulkInsertIntoTempTableAsync([new AutoIncrementTempTable { Name = "first" }, new AutoIncrementTempTable { Name = "second" }], tempTable, e => e.Name);

      var rows = await ActDbContext.Set<AutoIncrementTempTable>(EntityNameProvider.GetTempTableName(typeof(AutoIncrementTempTable)))
                                   .FromTempTable(new TempTableInfo(tempTable.Name))
                                   .AsNoTracking()
                                   .OrderBy(e => e.Id)
                                   .ToListAsync();
      rows.Select(e => (e.Id, e.Name)).Should().Equal((1, "first"), (2, "second"));
   }

   [Fact]
   public async Task Should_throw_when_creating_temp_table_with_auto_increment_if_it_is_not_primary_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<AutoIncrementTempTable>(false, b => b.HasKey(e => e.Id));

      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<AutoIncrementTempTable>(), _options))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("MySQL requires auto-increment property 'Id' to be part of the temporary table primary key.");

      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_not_create_auto_increment_column_for_regular_entities()
   {
      // A staging copy of a regular entity has to keep the explicit ids, i.e. 0 must not be replaced by a generated value.
      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithAutoIncrement>(), new TempTableCreationOptions());

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(TestEntityWithAutoIncrement.Id), "int", false);
      columns[0].Key.Should().Be("PRI");
      columns[0].Extra.Should().NotContain("auto_increment");
      ValidateColumn(columns[1], nameof(TestEntityWithAutoIncrement.Name), "longtext", true);
   }

   [Fact]
   public async Task Should_throw_if_temp_table_is_not_introduced()
   {
      await SUT.Awaiting(c => c.CreateTempTableAsync(GetTempTableEntityType<TempTable<ConvertibleClass>>(), _options))
               .Should().ThrowAsync<ArgumentException>();
   }

   [Fact]
   public async Task Should_throw_if_type_is_not_known_by_context()
   {
      await ActDbContext.Awaiting(ctx => ctx.CreateTempTableAsync<TestEntityTempTable>(_options))
                        .Should().ThrowAsync<EntityTypeNotFoundException>();
   }

   [Fact]
   public async Task Should_create_temp_table_via_context_extension_for_type()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var tempTable = await ActDbContext.CreateTempTableAsync(typeof(CustomTempTable), _options, CancellationToken.None);

      (await GetColumnsAsync(tempTable)).Select(c => c.Name).Should().Equal(nameof(CustomTempTable.Column1), nameof(CustomTempTable.Column2));
   }

   [Fact]
   public async Task Should_create_temp_table_with_truncate_if_exists()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = ReusingTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       DropTableOnDispose = false,
                       TruncateTableIfExists = false
                    };

      var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);
      await first.DisposeAsync();

      options.TruncateTableIfExists = true;
      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      try
      {
         tempTable.Name.Should().Be(first.Name);

         var columns = await GetColumnsAsync(tempTable);
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);

         // The table is dropped first and created again afterwards.
         var statements = GetTempTableStatements();
         statements.Should().HaveCount(3);
         statements[1].Should().Be($"DROP TEMPORARY TABLE IF EXISTS `{tempTable.Name}`;");
         statements[2].Should().StartWith($"CREATE TEMPORARY TABLE `{tempTable.Name}` (");
      }
      finally
      {
         await DropAndReleaseAsync(tempTable);
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_remove_rows_if_the_temp_table_is_created_again_with_truncate_if_exists()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = DefaultTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       DropTableOnDispose = false,
                       TruncateTableIfExists = true
                    };

      var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);
      await ActDbContext.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value")], first);
      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, first.Name)).Should().Be(1);

      var second = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      try
      {
         second.Name.Should().Be(first.Name);
         (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, second.Name)).Should().Be(0);
      }
      finally
      {
         await DropAndReleaseAsync(second);
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_throw_if_temp_table_exists_already_and_TruncateTableIfExists_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await ActDbContext.BulkInsertIntoTempTableAsync([new CustomTempTable(1, "value")], first);

      // 1050: table already exists
      var exception = await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
                               .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1050);

      // The failed attempt neither dropped the existing table nor released its connection.
      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
      (await MySqlTempTableInspector.CountRowsAsync(ActDbContext, first.Name)).Should().Be(1);
   }

   [Fact]
   public async Task Should_create_temp_table_with_truncate_if_exists_if_table_does_not_exist()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.TruncateTableIfExists = true;

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      (await GetColumnsAsync(tempTable)).Should().HaveCount(2);
   }

   [Fact]
   public async Task Should_use_name_of_custom_name_provider()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var provider = new FixedTempTableNameProvider("my temp `table`");
      var options = new TempTableCreationOptions { TableNameProvider = provider, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be("my temp `table`");
      provider.LeasedEntityTypes.Should().ContainSingle().Which.ClrType.Should().Be<CustomTempTable>();
      GetTempTableStatements().Single().Should().StartWith("CREATE TEMPORARY TABLE `my temp ``table``` (");
      (await GetColumnsAsync(tempTable)).Should().HaveCount(2);
      provider.DisposedLeases.Should().Be(0);

      await tempTable.DisposeAsync();

      provider.DisposedLeases.Should().Be(1);
   }

   [Fact]
   public async Task Should_release_name_lease_if_create_fails()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, typeBuilder => typeBuilder.HasKey(t => new { t.Column1, t.Column2 }));

      var provider = new FixedTempTableNameProvider("lease_released");
      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = provider,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.EntityTypeConfiguration,
                       PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1)
                    };

      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
               .Should().ThrowAsync<ArgumentException>();

      provider.DisposedLeases.Should().Be(1);
   }

   [Fact]
   public async Task Should_create_temp_table_with_reusable_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be("#CustomTempTable_1");

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
   }

   [Fact]
   public async Task Should_reuse_name_after_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;
      }

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be(firstName);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
   }

   [Fact]
   public async Task Should_reuse_name_after_it_is_freed_although_previously_not_dropped()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = ReusingTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       DropTableOnDispose = false
                    };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;
      }

      options.TruncateTableIfExists = true;
      var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      try
      {
         tempTable.Name.Should().Be(firstName);

         var columns = await GetColumnsAsync(tempTable);
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
      }
      finally
      {
         await DropAndReleaseAsync(tempTable);
         await ActDbContext.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_not_reuse_name_before_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      await using (var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
      {
         await using var second = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

         first.Name.Should().Be("#CustomTempTable_1");
         second.Name.Should().Be("#CustomTempTable_2");
         (await GetColumnsAsync(second)).Should().HaveCount(2);
      }
   }

   [Fact]
   public async Task Should_reuse_name_in_sorted_order()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;

         await using (await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
         {
         }
      }

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be(firstName);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "longtext", true);
   }

   [Fact]
   public async Task Should_open_connection()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new MySqlTestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      await using var tempTable = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_disposeAsync()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new MySqlTestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);
      await tempTableReference.DisposeAsync();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_dispose()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new MySqlTestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);
      tempTableReference.Dispose();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_even_if_ctx_is_disposed()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      ITempTableReference tempTableReference;

      await using (var ctx = new MySqlTestDbContext(options))
      {
         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         tempTableReference = await ctx.GetService<ITempTableCreator>()
                                       .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);
      }

      con.State.Should().Be(ConnectionState.Open);
      await tempTableReference.DisposeAsync();
      con.State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_table_ref_that_does_nothing_after_connection_is_disposed()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new MySqlTestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);
      await con.DisposeAsync();

      con.State.Should().Be(ConnectionState.Closed);
      await tempTableReference.DisposeAsync();
      con.State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_table_ref_that_does_nothing_after_connection_is_closed()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new MySqlTestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(MySqlTempTableInspector.GetTempTableEntityType<TestEntity>(ctx), _options);
      await con.CloseAsync();

      await tempTableReference.DisposeAsync();
      con.State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_remove_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var connection = await MySqlTempTableInspector.KeepConnectionOpenAsync(ActDbContext);

      var tempTableReference = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      (await MySqlTempTableInspector.ExistsAsync(ActDbContext, tempTableReference.Name)).Should().BeFalse();
   }

   [Fact]
   public async Task Should_throw_and_release_connection_and_name_when_cancellation_is_requested()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _options, cancellationTokenSource.Token))
               .Should().ThrowAsync<OperationCanceledException>();

      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), _options);
      tempTable.Name.Should().Be("#CustomTempTable_1");
   }

   [Fact]
   public async Task Should_not_commit_the_transaction_of_the_caller_when_temp_table_is_created_truncated_and_dropped()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      // CREATE TEMPORARY TABLE and DROP TEMPORARY TABLE don't end a transaction implicitly (as CREATE TABLE and DROP TABLE do).
      var options = new TempTableCreationOptions
                    {
                       TableNameProvider = DefaultTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       TruncateTableIfExists = true
                    };
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();
      ActDbContext.TestEntities.Add(new TestEntity { Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"), RequiredName = "Real" });
      await ActDbContext.SaveChangesAsync();

      await using (await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
      {
         await using (await SUT.CreateTempTableAsync(GetTempTableEntityType<CustomTempTable>(), options))
         {
         }
      }

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.RollbackAsync();

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_inlined_owned_type()
   {
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<TestEntity_Owns_Inline>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_temp_table_contains_inlined_owned_type()
   {
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<TestEntity_Owns_Inline>(e => new
                                                                                                                      {
                                                                                                                         e.Id,
                                                                                                                         e.InlineEntity
                                                                                                                      });
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<TestEntity_Owns_Inline>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("The entity 'Thinktecture.TestDatabaseContext.TestEntity_Owns_Inline' must not contain owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_separated_owned_type()
   {
      var ownerEntityType = GetTempTableEntityType<TestEntity_Owns_SeparateOne>();
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");

      // Other providers create a table for the owned type, MySQL rejects the owned type itself.
      var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("MySQL temporary tables do not support owned entities.");

      ActDbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_throw_when_selecting_separated_owned_type()
   {
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => new
                                                                                                                           {
                                                                                                                              e.Id,
                                                                                                                              e.SeparateEntity
                                                                                                                           });
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(GetTempTableEntityType<TestEntity_Owns_SeparateOne>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateOne' must not contain owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_many_owned_types()
   {
      var ownerEntityType = GetTempTableEntityType<TestEntity_Owns_SeparateMany>();
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");

      var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("MySQL temporary tables do not support owned entities.");
   }

   [Fact]
   public async Task Should_honor_collation()
   {
      var entityType = GetTempTableEntityType<TestEntityWithCollation>();
      await using var tempTable = await SUT.CreateTempTableAsync(entityType, _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(3);

      var databaseCollation = await ActDbContext.Database.SqlQueryRaw<string>("SELECT @@collation_database AS `Value`").SingleAsync();

      ValidateColumn(columns[0], nameof(TestEntityWithCollation.ColumnWithCollation), "varchar(100)", false, collation: "utf8mb4_bin");
      ValidateColumn(columns[1], nameof(TestEntityWithCollation.ColumnWithoutCollation), "varchar(100)", false, collation: databaseCollation);
      ValidateColumn(columns[2], nameof(TestEntityWithCollation.Id), "char(36)", false);
   }

   [Fact]
   public async Task Should_join_with_real_table_on_column_with_collation()
   {
      // Comparing columns with different collations fails with error 1267, so the temp table has to use the collation of the model.
      var id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730");
      ArrangeDbContext.TestEntitiesWithCollation.Add(new TestEntityWithCollation(id, "Value", "Value"));
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TestEntityWithCollation>(), new TempTableCreationOptions());
      await ActDbContext.BulkInsertIntoTempTableAsync([new TestEntityWithCollation(new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"), "value", "value"),
                                                       new TestEntityWithCollation(new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"), "Value", "Value")],
                                                      tempTable);

      var temp = ActDbContext.TestEntitiesWithCollation.FromTempTable(new TempTableInfo(tempTable.Name));
      var matches = await temp.Join(ActDbContext.TestEntitiesWithCollation, t => t.ColumnWithCollation, r => r.ColumnWithCollation, (t, r) => new { TempId = t.Id, RealId = r.Id })
                              .ToListAsync();

      // utf8mb4_bin is case sensitive: only 'Value' matches
      matches.Should().BeEquivalentTo([new { TempId = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"), RealId = id }]);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_complex_type()
   {
      var testEntity = GetTempTableEntityType<TestEntityWithComplexType>();

      await using var tempTable = await SUT.CreateTempTableAsync(testEntity, _options);

      var columns = (await GetColumnsAsync(tempTable)).OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
      columns.Should().HaveCount(3);
      ValidateColumn(columns[0], $"{nameof(TestEntityWithComplexType.Boundary)}_{nameof(BoundaryValueObject.Lower)}", "int", false);
      ValidateColumn(columns[1], $"{nameof(TestEntityWithComplexType.Boundary)}_{nameof(BoundaryValueObject.Upper)}", "int", false);
      ValidateColumn(columns[2], nameof(TestEntityWithComplexType.Id), "char(36)", false);
   }

   private IEntityType GetTempTableEntityType<T>()
   {
      return MySqlTempTableInspector.GetTempTableEntityType<T>(ActDbContext);
   }

   // ExecutedCommands contains the statements of the test setup as well.
   private IReadOnlyList<string> GetTempTableStatements()
   {
      return ExecutedCommands.Where(command => command.Contains("TEMPORARY TABLE", StringComparison.Ordinal)).ToList();
   }

   private Task<IReadOnlyList<MySqlTempTableColumn>> GetColumnsAsync(ITempTableReference tempTable)
   {
      return MySqlTempTableInspector.GetColumnsAsync(ActDbContext, tempTable.Name);
   }

   private async Task AssertColumnAsync<T>(string type, bool isNullable)
   {
      ConfigureModel = builder => builder.ConfigureTempTable<T>();

      await using var tempTable = await SUT.CreateTempTableAsync(GetTempTableEntityType<TempTable<T>>(), _options);

      var columns = await GetColumnsAsync(tempTable);
      columns.Should().HaveCount(1);
      ValidateColumn(columns[0], nameof(TempTable<T>.Column1), type, isNullable);
   }

   // A reference with DropTableOnDispose = false keeps the table and the connection open.
   private async Task DropAndReleaseAsync(ITempTableReference tempTable)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TEMPORARY TABLE IF EXISTS `{tempTable.Name}`");
      await ActDbContext.Database.CloseConnectionAsync();
   }

   private DbConnection CreateConnection()
   {
      return new MySqlConnection(ConnectionString);
   }

   private static DbContextOptions<MySqlTestDbContext> CreateOptions(DbConnection connection)
   {
      return new DbContextOptionsBuilder<MySqlTestDbContext>()
             .UseMySql(connection, MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport())
             .Options;
   }

   private static void ValidateColumn(
      MySqlTempTableColumn column,
      string name,
      string type,
      bool isNullable,
      string? defaultValue = null,
      string? collation = null)
   {
      ArgumentNullException.ThrowIfNull(column);

      column.Name.Should().Be(name);
      column.Type.Should().Be(type);
      column.IsNullable.Should().Be(isNullable);

      if (defaultValue is not null)
         column.Default.Should().Be(defaultValue);

      if (collation is not null)
         column.Collation.Should().Be(collation);
   }

   private sealed class AutoIncrementTempTable
   {
      public int Id { get; set; }
      public string? Name { get; set; }
   }

   private sealed class EnumTempTable
   {
      public string? Status { get; set; }
   }

   private sealed class FixedTempTableNameProvider(string name) : ITempTableNameProvider
   {
      private int _disposedLeases;

      public List<IEntityType> LeasedEntityTypes { get; } = [];
      public int DisposedLeases => _disposedLeases;

      public ITempTableNameLease LeaseName(DbContext ctx, IEntityType entityType)
      {
         LeasedEntityTypes.Add(entityType);
         return new FixedTempTableNameLease(name, () => Interlocked.Increment(ref _disposedLeases));
      }
   }

   private sealed class FixedTempTableNameLease(string name, Action onDispose) : ITempTableNameLease
   {
      public string Name { get; } = name;

      public void Dispose()
      {
         onDispose();
      }
   }
}
