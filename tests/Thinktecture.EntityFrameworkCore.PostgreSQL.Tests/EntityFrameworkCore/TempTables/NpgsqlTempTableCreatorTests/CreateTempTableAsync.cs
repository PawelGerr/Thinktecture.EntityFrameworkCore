using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.NpgsqlTempTableCreatorTests;

// ReSharper disable once InconsistentNaming
public class CreateTempTableAsync : IntegrationTestsBase
{
   private readonly NpgsqlTempTableCreationOptions _optionsWithNonUniqueName;
   private readonly string _connectionString;

   private NpgsqlTempTableCreator SUT => field ??= (NpgsqlTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreateTempTableAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
      _connectionString = npgsqlFixture.ConnectionString;
      _optionsWithNonUniqueName = new NpgsqlTempTableCreationOptions { TableNameProvider = DefaultTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };
   }

   [Fact]
   public async Task Should_create_temp_table_for_keyless_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_delete_temp_table_on_dispose_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
      {
      }

      AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                     .Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_not_delete_temp_table_on_dispose_if_DropTableOnDispose_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = false;

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
      {
      }

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_provided_column_only()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(1);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
   }

   [Fact]
   public async Task Should_create_pk_if_options_flag_is_set()
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;

      ConfigureModel = builder => builder.ConfigureTempTable<int, string>(typeBuilder => typeBuilder.Property(s => s.Column2).IsRequired().HasMaxLength(100));

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName);

      var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int, string>>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().ConstraintType.Should().Be("PRIMARY KEY");
   }

   [Fact]
   public async Task Should_create_temp_table_with_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "integer", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_nullable_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int?>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "integer", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "text", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_max_length()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(50));

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "character varying(50)", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_custom_collation()
   {
      await ArrangeDbContext.Database.ExecuteSqlRawAsync("""
         CREATE COLLATION IF NOT EXISTS ci
         (
             provider = icu,
             locale = 'und-u-ks-level2',
             deterministic = false
         );
         """);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(b =>
      {
         b.Property(e => e.Column2).UseCollation("ci");
      });

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true, collation: "ci");
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_collation_in_custom_schema()
   {
      await ArrangeDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE COLLATION IF NOT EXISTS "{Schema}"."ci_custom_schema"
         (
             provider = icu,
             locale = 'und-u-ks-level2',
             deterministic = false
         );
         """);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(b =>
      {
         b.Property(e => e.Column2).UseCollation($"{Schema}.ci_custom_schema");
      });

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true, collation: "ci_custom_schema");
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_collation_without_splitting()
   {
      // A collation without schema but a dot in the name
      await ArrangeDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE COLLATION IF NOT EXISTS "ci.no_split"
         (
             provider = icu,
             locale = 'und-u-ks-level2',
             deterministic = false
         );
         """);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(b =>
      {
         b.Property(e => e.Column2).UseCollation("ci.no_split");
      });

      _optionsWithNonUniqueName.SplitCollationComponents = false;

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true, collation: "ci.no_split");
   }

   [Fact]
   public async Task Should_create_temp_table_with_bool()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<bool>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<bool>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<bool>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "boolean", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_double()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<double>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<double>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<double>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "double precision", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_decimal()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<decimal>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "numeric(38,18)", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_2_columns()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int, string>>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "text", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_truncate_if_exists()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions
                    {
                       TableNameProvider = ReusingTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       DropTableOnDispose = false,
                       TruncateTableIfExists = false
                    };

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
      {
      }

      options.TruncateTableIfExists = true;
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

      var columns = AssertDbContext.GetTempTableColumns(tempTable.Name).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_open_connection()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      await using var tempTable = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_disposeAsync()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_dispose()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      tempTableReference.Dispose();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      var tempTableReference = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                     .Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_use_existing_transaction_in_disposeAsync()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using var tx = await ActDbContext.Database.BeginTransactionAsync();

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
      {
      }

      await tx.CommitAsync();

      AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                     .Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_not_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = false;

      var tempTableReference = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_reusable_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Contain("CustomTempTable_1");

      var columns = AssertDbContext.GetTempTableColumns(tempTable.Name).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_reuse_name_after_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;
      }

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be(firstName);

      var columns = AssertDbContext.GetTempTableColumns(tempTable.Name).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_reuse_name_after_it_is_freed_although_previously_not_dropped()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions
                    {
                       TableNameProvider = ReusingTempTableNameProvider.Instance,
                       PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None,
                       DropTableOnDispose = false
                    };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;
      }

      options.TruncateTableIfExists = true;
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be(firstName);

      var columns = AssertDbContext.GetTempTableColumns(tempTable.Name).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   [Fact]
   public async Task Should_not_reuse_name_before_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      await using (var first = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
      {
         await using var second = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

         second.Name.Should().NotBe(first.Name);
      }
   }

   [Fact]
   public async Task Should_reuse_name_in_sorted_order()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new NpgsqlTempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      string firstName;

      await using (var first = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
      {
         firstName = first.Name;

         await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options))
         {
         }
      }

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options);

      tempTable.Name.Should().Be(firstName);

      var columns = AssertDbContext.GetTempTableColumns(tempTable.Name).ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "integer", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "text", true);
   }

   private DbConnection CreateConnection()
   {
      return new NpgsqlConnection(_connectionString);
   }

   private static DbContextOptions<TestDbContext> CreateOptions(DbConnection connection)
   {
      var builder = new DbContextOptionsBuilder<TestDbContext>();
      builder.UseNpgsql(connection, npgsqlOptions => npgsqlOptions.AddBulkOperationSupport());
      builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      return builder.Options;
   }

   private static void ValidateColumn(
      PgTempTableColumn column,
      string name,
      string type,
      bool isNullable,
      string? defaultValue = null,
      string? collation = null)
   {
      ArgumentNullException.ThrowIfNull(column);

      column.ColumnName.Should().Be(name);
      column.DataType.Should().Be(type);
      column.IsNullable.Should().Be(isNullable);

      if (defaultValue is not null)
         column.ColumnDefault.Should().Be(defaultValue);

      if (collation is not null)
         column.CollationName.Should().Be(collation);
   }
}
