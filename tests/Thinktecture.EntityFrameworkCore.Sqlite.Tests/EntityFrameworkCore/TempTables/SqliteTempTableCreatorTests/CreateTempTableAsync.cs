using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqliteTempTableCreatorTests
{
   // ReSharper disable once InconsistentNaming
   public class CreateTempTableAsync : IntegrationTestsBase
   {
      private readonly Mock<ISqlGenerationHelper> _sqlGenerationHelperMock;
      private readonly Mock<IRelationalTypeMappingSource> _relationalTypeMappingSourceMock;
      private readonly TempTableCreationOptions _optionsWithNonUniqueNameAndNoPrimaryKey;

      private SqliteTempTableCreator? _sut;

      private SqliteTempTableCreator SUT => _sut ??= new SqliteTempTableCreator(ActDbContext.GetService<ICurrentDbContext>(),
                                                                                ActDbContext.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>(),
                                                                                _sqlGenerationHelperMock.Object,
                                                                                _relationalTypeMappingSourceMock.Object,
                                                                                new TempTableStatementCache<SqliteTempTableCreatorCacheKey>());

      public CreateTempTableAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _sqlGenerationHelperMock = new Mock<ISqlGenerationHelper>();
         _sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>(), It.IsAny<string>()))
                                 .Returns<string, string>((name, schema) => schema == null ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"");
         _sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>()))
                                 .Returns<string>(name => $"\"{name}\"");
         _relationalTypeMappingSourceMock = new Mock<IRelationalTypeMappingSource>();

         _optionsWithNonUniqueNameAndNoPrimaryKey = new TempTableCreationOptions { TableNameProvider = DefaultTempTableNameProvider.Instance, PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None };
      }

      [Fact]
      public async Task Should_create_temp_table_for_keyless_entity()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_delete_temp_table_on_dispose_if_DropTableOnDispose_is_true()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         _optionsWithNonUniqueNameAndNoPrimaryKey.DropTableOnDispose = true;

         // ReSharper disable once UseAwaitUsing
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey).ConfigureAwait(false))
         {
         }

         AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                        .Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_true()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         _optionsWithNonUniqueNameAndNoPrimaryKey.DropTableOnDispose = true;

         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey).ConfigureAwait(false))
         {
         }

         AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList()
                        .Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_not_delete_temp_table_on_dispose_if_DropTableOnDispose_is_false()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         _optionsWithNonUniqueNameAndNoPrimaryKey.DropTableOnDispose = false;

         // ReSharper disable once UseAwaitUsing
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey).ConfigureAwait(false))
         {
         }

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_not_delete_temp_table_on_disposeAsync_if_DropTableOnDispose_is_false()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         _optionsWithNonUniqueNameAndNoPrimaryKey.DropTableOnDispose = false;

         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey).ConfigureAwait(false))
         {
         }

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_create_temp_table_with_reusable_name()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance };

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_reuse_name_after_it_is_freed()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance };

         // ReSharper disable once RedundantArgumentDefaultValue
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false))
         {
         }

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_reuse_name_after_it_is_freed_although_previously_not_dropped()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var options = new TempTableCreationOptions
                       {
                          TableNameProvider = ReusingTempTableNameProvider.Instance,
                          DropTableOnDispose = false
                       };

         // ReSharper disable once RedundantArgumentDefaultValue
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false))
         {
         }

         options.TruncateTableIfExists = true;
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);

         AssertDbContext.GetTempTableColumns("#CustomTempTable_2").ToList()
                        .Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_not_reuse_name_before_it_is_freed()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance };

         // ReSharper disable once RedundantArgumentDefaultValue
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false))
         {
            await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false);
         }

         var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_2").ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_reuse_name_in_sorted_order()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         var options = new TempTableCreationOptions { TableNameProvider = ReusingTempTableNameProvider.Instance };

         // #CustomTempTable_1
         await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false))
         {
            // #CustomTempTable_2
            await using (await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false))
            {
            }
         }

         // #CustomTempTable_1
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), options).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns("#CustomTempTable_1").ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "TEXT", true);
      }

      [Fact]
      public async Task Should_create_temp_table_with_provided_column_only()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         _optionsWithNonUniqueNameAndNoPrimaryKey.PropertiesToInclude = EntityPropertiesProvider.From<CustomTempTable>(t => t.Column1);

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(1);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "INTEGER", false);
      }

      [Fact]
      public async Task Should_create_pk_if_options_flag_is_set()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PrimaryKeyCreation = PrimaryKeyPropertiesProviders.AdaptiveForced;

         ConfigureModel = builder => builder.ConfigureTempTable<int, string>().Property(s => s.Column2).HasMaxLength(100);

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int, string>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var keyColumns = await AssertDbContext.GetTempTableKeyColumns<TempTable<int, string>>().ToListAsync();
         keyColumns.Should().HaveCount(2);
         keyColumns[0].Name.Should().Be(nameof(TempTable<int, string>.Column1));
         keyColumns[1].Name.Should().Be(nameof(TempTable<int, string>.Column2));
      }

      [Fact]
      public async Task Should_throw_if_some_pk_columns_are_missing()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PrimaryKeyCreation = PrimaryKeyPropertiesProviders.EntityTypeConfiguration;
         _optionsWithNonUniqueNameAndNoPrimaryKey.PropertiesToInclude = EntityPropertiesProvider.From<CustomTempTable>(t => t.Column1);

         ConfigureModel = builder =>
                          {
                             var entity = builder.ConfigureTempTableEntity<CustomTempTable>(false);
                             entity.Property(s => s.Column2).HasMaxLength(100);
                             entity.HasKey(s => new { s.Column1, s.Column2 });
                          };

         // ReSharper disable once RedundantArgumentDefaultValue
         await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey))
                  .Should().ThrowAsync<ArgumentException>().WithMessage(@"Cannot create PRIMARY KEY because not all key columns are part of the temp table.
You may use other key properties providers like 'PrimaryKeyPropertiesProviders.AdaptiveEntityTypeConfiguration' instead of 'PrimaryKeyPropertiesProviders.EntityTypeConfiguration' to get different behaviors.
Missing columns: Column2.");
      }

      [Fact]
      public async Task Should_not_throw_if_some_pk_columns_are_missing_and_provider_is_Adaptive()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PrimaryKeyCreation = PrimaryKeyPropertiesProviders.AdaptiveForced;
         _optionsWithNonUniqueNameAndNoPrimaryKey.PropertiesToInclude = EntityPropertiesProvider.From<CustomTempTable>(t => t.Column1);

         ConfigureModel = builder =>
                          {
                             var entity = builder.ConfigureTempTableEntity<CustomTempTable>(false);
                             entity.Property(s => s.Column2).HasMaxLength(100);
                             entity.HasKey(s => new { s.Column1, s.Column2 });
                          };

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var keyColumns = await AssertDbContext.GetTempTableKeyColumns<CustomTempTable>().ToListAsync();
         keyColumns.Should().HaveCount(1);
         keyColumns[0].Name.Should().Be(nameof(CustomTempTable.Column1));
      }

      [Fact]
      public async Task Should_open_connection()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await ctx.GetService<ITempTableCreator>()
                                              .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
      }

      [Fact]
      public async Task Should_return_reference_to_be_able_to_close_connection()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         await tempTableReference.DisposeAsync();

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_reference_to_be_able_to_close_connection_event_if_ctx_is_disposed()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         ITempTableReference tempTableReference;

         await using (var ctx = new TestDbContext(builder.Options))
         {
            ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

            // ReSharper disable once RedundantArgumentDefaultValue
            tempTableReference = await ctx.GetService<ITempTableCreator>()
                                          .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         }

         con.State.Should().Be(ConnectionState.Open);
         await tempTableReference.DisposeAsync();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_table_ref_that_does_nothing_after_connection_is_disposed()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         con.Dispose();

         con.State.Should().Be(ConnectionState.Closed);
         tempTableReference.Dispose();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_table_ref_that_does_nothing_after_connection_is_disposedAsync()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                           .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         await con.DisposeAsync();

         con.State.Should().Be(ConnectionState.Closed);
         await tempTableReference.DisposeAsync();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_table_ref_that_does_nothing_after_connection_is_closed()
      {
         await using var con = new SqliteConnection("DataSource=:memory:");

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTableReference = await ctx.GetService<ITempTableCreator>()
                                                       .CreateTempTableAsync(ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         con.Close();

         tempTableReference.Dispose();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_reference_to_remove_temp_table()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTableReference = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueNameAndNoPrimaryKey);
         await tempTableReference.DisposeAsync();

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().BeEmpty();
      }

      [Fact]
      public async Task Should_create_temp_table_for_entityType()
      {
         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntity>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity>().OrderBy(c => c.Name).ToList();
         columns.Should().HaveCount(7);

         ValidateColumn(columns[0], nameof(TestEntity.ConvertibleClass), "INTEGER", true);
         ValidateColumn(columns[1], nameof(TestEntity.Count), "INTEGER", false);
         ValidateColumn(columns[2], nameof(TestEntity.Id), "TEXT", false);
         ValidateColumn(columns[3], nameof(TestEntity.Name), "TEXT", true);
         ValidateColumn(columns[4], nameof(TestEntity.ParentId), "TEXT", true);
         ValidateColumn(columns[5], nameof(TestEntity.PropertyWithBackingField), "INTEGER", false);
         ValidateColumn(columns[6], "_privateField", "INTEGER", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_autoincrement_if_it_is_primary_key()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PrimaryKeyCreation = PrimaryKeyPropertiesProviders.EntityTypeConfiguration;

         // ReSharper disable once RedundantArgumentDefaultValue
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntityWithAutoIncrement>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntityWithAutoIncrement>().OrderBy(c => c.Name).ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(TestEntity.Id), "INTEGER", false);
         ValidateColumn(columns[1], nameof(TestEntity.Name), "TEXT", true);
      }

      [Fact]
      public async Task Should_throw_when_creating_temp_table_with_autoincrement_if_it_is_not_primary_key()
      {
         // ReSharper disable once RedundantArgumentDefaultValue
         await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntityWithAutoIncrement>(), _optionsWithNonUniqueNameAndNoPrimaryKey))
                  .Should().ThrowAsync<NotSupportedException>()
                  .WithMessage(@"SQLite does not allow the property 'Id' of the entity 'Thinktecture.TestDatabaseContext.TestEntityWithAutoIncrement' to be an AUTOINCREMENT column unless this column is the PRIMARY KEY.
Currently configured primary keys: []");
      }

      [Fact]
      public async Task Should_throw_if_temp_table_is_not_introduced()
      {
         await SUT.Awaiting(c => c.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueNameAndNoPrimaryKey))
                  .Should().ThrowAsync<ArgumentException>();
      }

      [Fact]
      public async Task Should_create_temp_table_with_one_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList().Should().HaveCount(1);
      }

      [Fact]
      public async Task Should_create_temp_table_without_primary_key()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();
         _optionsWithNonUniqueNameAndNoPrimaryKey.PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None;

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var constraints = await AssertDbContext.GetTempTableKeyColumns<TempTable<int>>().ToListAsync();
         constraints.Should().BeEmpty();
      }

      [Fact]
      public async Task Should_create_temp_table_with_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "INTEGER", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int?>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "INTEGER", true);
      }

      [Fact]
      public async Task Should_create_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>().Property(t => t.Column1).IsRequired();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int?>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "INTEGER", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_double()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<double>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<double>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<double>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "REAL", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<decimal>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "TEXT", false); // decimal is stored as TEXT (see SqliteTypeMappingSource)
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>().Property(t => t.Column1).HasColumnType("decimal(20,5)");

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<decimal>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal(20,5)", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_bool()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<bool>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<bool>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<bool>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "INTEGER", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<string>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "TEXT", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string_with_max_length()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).HasMaxLength(50);

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<string>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "TEXT", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_2_columns()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TempTable<int, string>>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int, string>>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "INTEGER", false);
         ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "TEXT", false);
      }

      [Fact]
      public async Task Should_create_temp_table_for_entity_with_inlined_owned_type()
      {
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntity_Owns_Inline>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity_Owns_Inline>().ToList();
         columns.Should().HaveCount(3);

         ValidateColumn(columns[0], nameof(TestEntity_Owns_Inline.Id), "TEXT", false);
         ValidateColumn(columns[1], $"{nameof(TestEntity_Owns_Inline.InlineEntity)}_{nameof(TestEntity_Owns_Inline.InlineEntity.IntColumn)}", "INTEGER", false);
         ValidateColumn(columns[2], $"{nameof(TestEntity_Owns_Inline.InlineEntity)}_{nameof(TestEntity_Owns_Inline.InlineEntity.StringColumn)}", "TEXT", true);
      }

      [Fact]
      public async Task Should_create_temp_table_for_entity_by_selecting_inlined_owned_type()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PropertiesToInclude = EntityPropertiesProvider.From<TestEntity_Owns_Inline>(e => new
                                                                                                                                   {
                                                                                                                                      e.Id,
                                                                                                                                      e.InlineEntity
                                                                                                                                   });
         await using var tempTable = await SUT.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntity_Owns_Inline>(), _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity_Owns_Inline>().ToList();
         columns.Should().HaveCount(3);

         ValidateColumn(columns[0], nameof(TestEntity_Owns_Inline.Id), "TEXT", false);
         ValidateColumn(columns[1], $"{nameof(TestEntity_Owns_Inline.InlineEntity)}_{nameof(TestEntity_Owns_Inline.InlineEntity.IntColumn)}", "INTEGER", false);
         ValidateColumn(columns[2], $"{nameof(TestEntity_Owns_Inline.InlineEntity)}_{nameof(TestEntity_Owns_Inline.InlineEntity.StringColumn)}", "TEXT", true);
      }

      [Fact]
      public async Task Should_create_temp_table_for_entity_with_separated_owned_type()
      {
         var ownerEntityType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>();
         await using var tempTable = await SUT.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity_Owns_SeparateOne>().ToList();
         columns.Should().HaveCount(1);

         ValidateColumn(columns[0], nameof(TestEntity_Owns_SeparateOne.Id), "TEXT", false);

         var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
         await using var ownedTempTable = await SUT.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueNameAndNoPrimaryKey);

         columns = AssertDbContext.GetTempTableColumns(ownedTypeEntityType).ToList();
         columns.Should().HaveCount(3);
         ValidateColumn(columns[0], $"{nameof(TestEntity_Owns_SeparateOne)}{nameof(TestEntity_Owns_SeparateOne.Id)}", "TEXT", false);
         ValidateColumn(columns[1], nameof(OwnedEntity.IntColumn), "INTEGER", false);
         ValidateColumn(columns[2], nameof(OwnedEntity.StringColumn), "TEXT", true);
      }

      [Fact]
      public async Task Should_throw_when_selecting_separated_owned_type()
      {
         _optionsWithNonUniqueNameAndNoPrimaryKey.PropertiesToInclude = EntityPropertiesProvider.From<TestEntity_Owns_SeparateOne>(e => new
                                                                                                                                        {
                                                                                                                                           e.Id,
                                                                                                                                           e.SeparateEntity
                                                                                                                                        });
         await SUT.Awaiting(sut => sut.CreateTempTableAsync(ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>(), _optionsWithNonUniqueNameAndNoPrimaryKey))
                  .Should().ThrowAsync<NotSupportedException>()
                  .WithMessage("Properties of owned types that are saved in a separate table are not supported. Property: SeparateEntity");
      }

      [Fact]
      public async Task Should_create_temp_table_for_entity_with_many_owned_types()
      {
         var ownerEntityType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateMany>();
         await using var tempTable = await SUT.CreateTempTableAsync(ownerEntityType, _optionsWithNonUniqueNameAndNoPrimaryKey);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity_Owns_SeparateMany>().ToList();
         columns.Should().HaveCount(1);

         ValidateColumn(columns[0], nameof(TestEntity_Owns_SeparateMany.Id), "TEXT", false);

         var ownedTypeEntityType = ownerEntityType.GetNavigations().Single().TargetEntityType;
         await using var ownedTempTable = await SUT.CreateTempTableAsync(ownedTypeEntityType, _optionsWithNonUniqueNameAndNoPrimaryKey);

         columns = AssertDbContext.GetTempTableColumns(ownedTypeEntityType).ToList();
         columns.Should().HaveCount(4);
         ValidateColumn(columns[0], $"{nameof(TestEntity_Owns_SeparateMany)}{nameof(TestEntity_Owns_SeparateMany.Id)}", "TEXT", false);
         ValidateColumn(columns[1], "Id", "INTEGER", false);
         ValidateColumn(columns[2], nameof(OwnedEntity.IntColumn), "INTEGER", false);
         ValidateColumn(columns[3], nameof(OwnedEntity.StringColumn), "TEXT", true);
      }

      private static void ValidateColumn(SqliteTableInfo column, string name, string type, bool isNullable)
      {
         if (column == null)
            throw new ArgumentNullException(nameof(column));

         column.Name.Should().Be(name);
         column.Type.Should().Be(type);
         column.NotNull.Should().Be(isNullable ? 0 : 1);
      }
   }
}
