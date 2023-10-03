using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests;

// ReSharper disable once InconsistentNaming
public class CreateTempTableAsync : IntegrationTestsBase
{
   private readonly SqlServerTempTableCreationOptions _optionsWithNonUniqueName;
   private readonly string _connectionString;

   private SqlServerTempTableCreator? _sut;
   private SqlServerTempTableCreator SUT => _sut ??= (SqlServerTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreateTempTableAsync(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
      _connectionString = sqlServerFixture.ConnectionString;
      _optionsWithNonUniqueName = new SqlServerTempTableCreationOptions { TableNameProvider = DefaultTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };
   }

   [Fact]
   public async Task Should_create_temp_table_for_keyless_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_delete_temp_table_on_dispose_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false))
      {
      }

      AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                     .Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_true()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = true;

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false))
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

      // ReSharper disable once UseAwaitUsing
      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false))
      {
      }

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_not_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_false()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.DropTableOnDispose = false;

      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false))
      {
      }

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_reusable_name()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false);

      var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_reuse_name_after_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      // ReSharper disable once RedundantArgumentDefaultValue
      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false))
      {
      }

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false);

      var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
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

      // ReSharper disable once RedundantArgumentDefaultValue
      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false))
      {
      }

      options.TruncateTableIfExists = true;
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false);

      var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);

      AssertDbContext.GetTempTableColumns("#CustomTempTable_2").ToList()
                     .Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_not_reuse_name_before_it_is_freed()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      // ReSharper disable once RedundantArgumentDefaultValue
      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false))
      {
         await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false);
      }

      var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_2").ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_reuse_name_in_sorted_order()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance, PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None };

      // #CustomTempTable_1
      await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false))
      {
         // #CustomTempTable_2
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false))
         {
         }
      }

      // #CustomTempTable_1
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), options).ConfigureAwait(false);

      var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_provided_column_only()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(1);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
   }

   [Fact]
   public async Task Should_create_pk_if_options_flag_is_set()
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;

      ConfigureModel = builder => builder.ConfigureTempTable<int, string>(typeBuilder => typeBuilder.Property(s => s.Column2).IsRequired().HasMaxLength(100));

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName);

      var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int, string>>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

      var keyColumns = await AssertDbContext.GetTempTableKeyColumns<TempTable<int, string>>().ToListAsync();
      keyColumns.Should().HaveCount(2);
      keyColumns[0].COLUMN_NAME.Should().Be(nameof(TempTable<int, string>.Column1));
      keyColumns[1].COLUMN_NAME.Should().Be(nameof(TempTable<int, string>.Column2));
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

      // ReSharper disable once RedundantArgumentDefaultValue
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<ArgumentException>().WithMessage("""
                                                                     Cannot create PRIMARY KEY because not all key columns are part of the temp table.
                                                                     You may use other key properties providers like 'IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration' instead of 'IPrimaryKeyPropertiesProvider.EntityTypeConfiguration' to get different behaviors.
                                                                     Missing columns: Column2.
                                                                     """);
   }

   [Fact]
   public async Task Should_not_throw_if_some_pk_columns_are_missing_and_provider_is_Adaptive()
   {
      _optionsWithNonUniqueName.PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.AdaptiveForced;
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<CustomTempTable>(t => t.Column1);

      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>(false, typeBuilder =>
                                                                                           {
                                                                                              typeBuilder.Property(s => s.Column2).HasMaxLength(100);
                                                                                              typeBuilder.HasKey(s => new { s.Column1, s.Column2 });
                                                                                           });

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);

      var keyColumns = await AssertDbContext.GetTempTableKeyColumns<CustomTempTable>().ToListAsync();
      keyColumns.Should().HaveCount(1);
      keyColumns[0].COLUMN_NAME.Should().Be(nameof(CustomTempTable.Column1));
   }

   [Fact]
   public async Task Should_open_connection()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options, CreateDefaultSchema());

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_dispose()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options, CreateDefaultSchema());

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // ReSharper disable once RedundantArgumentDefaultValue
      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      tempTableReference.Dispose();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_using_disposeAsync()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options, CreateDefaultSchema());

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // ReSharper disable once RedundantArgumentDefaultValue
      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_be_able_to_close_connection_even_if_ctx_is_disposed()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      ITempTableReference tempTableReference;

      await using (var ctx = new TestDbContext(options, CreateDefaultSchema()))
      {
         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         tempTableReference = await ctx.GetService<ITempTableCreator>()
                                       .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
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

      await using var ctx = new TestDbContext(options, CreateDefaultSchema());

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // ReSharper disable once RedundantArgumentDefaultValue
      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      con.Dispose();

      con.State.Should().Be(ConnectionState.Closed);
      await tempTableReference.DisposeAsync();
      con.State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_table_ref_that_does_nothing_after_connection_is_closed()
   {
      await using var con = CreateConnection();

      var options = CreateOptions(con);

      await using var ctx = new TestDbContext(options, CreateDefaultSchema());

      ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // ReSharper disable once RedundantArgumentDefaultValue
      var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                        .CreateTempTableAsync(ctx.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);
      await con.CloseAsync();

      await tempTableReference.DisposeAsync();
      con.State.Should().Be(ConnectionState.Closed);
   }

   [Fact]
   public async Task Should_return_reference_to_remove_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

      // ReSharper disable once RedundantArgumentDefaultValue
      var tempTableReference = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName);
      await tempTableReference.DisposeAsync();

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_create_temp_table_for_entityType()
   {
      // ReSharper disable once RedundantArgumentDefaultValue
      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TestEntity>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TestEntity>().OrderBy(c => c.COLUMN_NAME).ToList();
      columns.Should().HaveCount(8);

      ValidateColumn(columns[0], "_privateField", "int", false);
      ValidateColumn(columns[1], nameof(TestEntity.ConvertibleClass), "int", true);
      ValidateColumn(columns[2], nameof(TestEntity.Count), "int", false);
      ValidateColumn(columns[3], nameof(TestEntity.Id), "uniqueidentifier", false);
      ValidateColumn(columns[4], nameof(TestEntity.Name), "nvarchar", true);
      ValidateColumn(columns[5], nameof(TestEntity.ParentId), "uniqueidentifier", true);
      ValidateColumn(columns[6], nameof(TestEntity.PropertyWithBackingField), "int", false);
      ValidateColumn(columns[7], nameof(TestEntity.RequiredName), "nvarchar", false);
   }

   [Fact]
   public async Task Should_throw_if_temp_table_is_not_introduced()
   {
      await SUT.Awaiting(c => c.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<ConvertibleClass>>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<ArgumentException>();
   }

   [Fact]
   public async Task Should_create_temp_table_with_one_column()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int>>(), _optionsWithNonUniqueName);

      AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList().Should().HaveCount(1);
   }

   [Fact]
   public async Task Should_create_temp_table_without_primary_key()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int>>(), _optionsWithNonUniqueName);

      var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int>>().ToListAsync();
      constraints.Should().HaveCount(0);
   }

   [Fact]
   public async Task Should_create_temp_table_with_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_nullable_int()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>();

      await using var temptTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int?>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", true);
   }

   [Fact]
   public async Task Should_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int?>(typeBuilder => typeBuilder.Property(t => t.Column1).IsRequired());

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int?>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_double()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<double>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<double>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<double>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "float", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_decimal()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<decimal>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<decimal>(typeBuilder => typeBuilder.Property(t => t.Column1).HasPrecision(20, 5));

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<decimal>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false, 20, 5);
   }

   [Fact]
   public async Task Should_create_temp_table_with_bool()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<bool>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<bool>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<bool>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "bit", false);
   }

   [Fact]
   public async Task Should_create_temp_table_with_string()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_converter_and_default_value()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<ConvertibleClass>(typeBuilder => typeBuilder.Property(t => t.Column1)
                                                                                                         .HasConversion(c => c.Key, k => new ConvertibleClass(k))
                                                                                                         .HasDefaultValue(new ConvertibleClass(1)));

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<ConvertibleClass>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<ConvertibleClass>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<ConvertibleClass>.Column1), "int", true, defaultValue: "((1))");
   }

   [Fact]
   public async Task Should_create_temp_table_with_string_with_max_length()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<string>(typeBuilder => typeBuilder.Property(t => t.Column1).HasMaxLength(50));

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
      ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true, charMaxLength: 50);
   }

   [Fact]
   public async Task Should_create_temp_table_with_2_columns()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

      await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TempTable<int, string>>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "int", false);
      ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_create_temp_table_with_default_database_collation()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();
      _optionsWithNonUniqueName.UseDefaultDatabaseCollation = true;

      await SUT.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false);

      var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
      columns.Should().HaveCount(2);

      ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
      ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_inlined_owned_type()
   {
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TestEntity_Owns_Inline>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_separated_owned_type()
   {
      var ownerEntityType = ActDbContext.GetTempTableEntityType<TestEntity_Owns_SeparateOne>();
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");

      var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
      await using var ownedTempTable = await SUT.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns(ownedTypeEntityType).ToList();
      columns.Should().HaveCount(3);
      ValidateColumn(columns[0], $"{nameof(TestEntity_Owns_SeparateOne)}{nameof(TestEntity_Owns_SeparateOne.Id)}", "uniqueidentifier", false);
      ValidateColumn(columns[1], nameof(OwnedEntity.IntColumn), "int", false);
      ValidateColumn(columns[2], nameof(OwnedEntity.StringColumn), "nvarchar", true);
   }

   [Fact]
   public async Task Should_throw_when_selecting_separated_owned_type()
   {
      _optionsWithNonUniqueName.PropertiesToInclude = IEntityPropertiesProvider.Include<TestEntity_Owns_SeparateOne>(e => new
                                                                                                                          {
                                                                                                                             e.Id,
                                                                                                                             e.SeparateEntity
                                                                                                                          });
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetTempTableEntityType<TestEntity_Owns_SeparateOne>(), _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>()
               .WithMessage("The entity 'Thinktecture.TestDatabaseContext.TestEntity_Owns_SeparateOne' must not contain owned entities.");
   }

   [Fact]
   public async Task Should_throw_if_temp_table_entity_contains_many_owned_types()
   {
      var ownerEntityType = ActDbContext.GetTempTableEntityType<TestEntity_Owns_SeparateMany>();
      await SUT.Awaiting(sut => sut.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueName))
               .Should().ThrowAsync<NotSupportedException>().WithMessage("Temp tables don't support owned entities.");

      var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
      await using var ownedTempTable = await SUT.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns(ownedTypeEntityType).ToList();
      columns.Should().HaveCount(4);
      ValidateColumn(columns[0], $"{nameof(TestEntity_Owns_SeparateMany)}{nameof(TestEntity_Owns_SeparateMany.Id)}", "uniqueidentifier", false);
      ValidateColumn(columns[1], "Id", "int", false);
      ValidateColumn(columns[2], nameof(OwnedEntity.IntColumn), "int", false);
      ValidateColumn(columns[3], nameof(OwnedEntity.StringColumn), "nvarchar", true);
   }

   [Fact]
   public async Task Should_honor_collation()
   {
      var entityType = ActDbContext.GetTempTableEntityType<TestEntityWithCollation>();
      await using var tempTable = await SUT.CreateTempTableAsync(entityType, _optionsWithNonUniqueName);

      var columns = AssertDbContext.GetTempTableColumns<TestEntityWithCollation>().ToList();
      columns.Should().HaveCount(3);

      var connection = AssertDbContext.Database.GetDbConnection();
      await using var command = connection.CreateCommand();
      command.Transaction = AssertDbContext.Database.CurrentTransaction?.GetDbTransaction();
      command.CommandText = "SELECT CONVERT (varchar(256), SERVERPROPERTY('collation'))";
      var databaseCollation = (string?)await command.ExecuteScalarAsync() ?? throw new Exception("Couldn't fetch database collection.");

      ValidateColumn(columns[0], nameof(TestEntityWithCollation.Id), "uniqueidentifier", false);
      ValidateColumn(columns[1], nameof(TestEntityWithCollation.ColumnWithCollation), "nvarchar", false, collation: "Japanese_CI_AS");
      ValidateColumn(columns[2], nameof(TestEntityWithCollation.ColumnWithoutCollation), "nvarchar", false, collation: databaseCollation);
   }

   [Fact]
   public async Task Should_create_temp_table_for_entity_with_complex_type()
   {
      var testEntity = ActDbContext.GetTempTableEntityType<TestEntityWithComplexType>();

      await using var tempTable = await SUT.CreateTempTableAsync(testEntity, _optionsWithNonUniqueName);

      var columns = await AssertDbContext.GetTempTableColumns(testEntity).ToListAsync();
      columns.Should().HaveCount(3);
      ValidateColumn(columns[0], nameof(TestEntityWithComplexType.Id), "uniqueidentifier", false);
      ValidateColumn(columns[1], $"{nameof(TestEntityWithComplexType.Boundary)}_{nameof(BoundaryValueObject.Lower)}", "int", false);
      ValidateColumn(columns[2], $"{nameof(TestEntityWithComplexType.Boundary)}_{nameof(BoundaryValueObject.Upper)}", "int", false);
   }

   private DbConnection CreateConnection()
   {
      return new SqlConnection(_connectionString);
   }

   private DbContextOptions<TestDbContext> CreateOptions(DbConnection connection)
   {
      return TestCtxProviderBuilder.CreateOptionsBuilder(connection, Schema).Options;
   }

   private static void ValidateColumn(
      InformationSchemaColumn column,
      string name,
      string type,
      bool isNullable,
      byte? numericPrecision = null,
      int? numericScale = null,
      int? charMaxLength = null,
      string? defaultValue = null,
      string? collation = null)
   {
      ArgumentNullException.ThrowIfNull(column);

      column.COLUMN_NAME.Should().Be(name);
      column.DATA_TYPE.Should().Be(type);
      column.IS_NULLABLE.Should().Be(isNullable ? "YES" : "NO");
      column.COLUMN_DEFAULT.Should().Be(defaultValue);

      if (collation is not null)
         column.COLLATION_NAME.Should().Be(collation);

      if (numericPrecision.HasValue)
         column.NUMERIC_PRECISION.Should().Be(numericPrecision.Value);

      if (numericScale.HasValue)
         column.NUMERIC_SCALE.Should().Be(numericScale.Value);

      if (charMaxLength.HasValue)
         column.CHARACTER_MAXIMUM_LENGTH.Should().Be(charMaxLength.Value);
   }

   private DbDefaultSchema? CreateDefaultSchema()
   {
      return Schema is null ? null : new DbDefaultSchema(Schema);
   }
}
