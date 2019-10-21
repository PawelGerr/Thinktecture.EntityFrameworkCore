using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests
{
   // ReSharper disable once InconsistentNaming
   public class CreateTempTableAsync : IntegrationTestsBase
   {
      private readonly SqlServerTempTableCreator _sut;
      private readonly TempTableCreationOptions _optionsWithNonUniqueName;

      public CreateTempTableAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
         var sqlGenerationHelperMock = new Mock<ISqlGenerationHelper>();
         var relationalTypeMappingSourceMock = new Mock<IRelationalTypeMappingSource>();
         sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>(), It.IsAny<string>()))
                                .Returns<string, string>((name, schema) => schema == null ? $"[{name}]" : $"[{schema}].[{name}]");
         sqlGenerationHelperMock.Setup(h => h.DelimitIdentifier(It.IsAny<string>()))
                                .Returns<string>(name => $"[{name}]");
         _sut = new SqlServerTempTableCreator(sqlGenerationHelperMock.Object, relationalTypeMappingSourceMock.Object);
         _optionsWithNonUniqueName = new TempTableCreationOptions { MakeTableNameUnique = false, CreatePrimaryKey = false };
      }

      [Fact]
      public async Task Should_create_temp_table_for_keyless_entity()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
      }

      [Fact]
      public async Task Should_create_pk_if_options_flag_is_set()
      {
         _optionsWithNonUniqueName.CreatePrimaryKey = true;

         ConfigureModel = builder => builder.ConfigureTempTable<int, string>().Property(s => s.Column2).HasMaxLength(100);

         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int, string>>().ToListAsync();
         constraints.Should().HaveCount(1)
                    .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

         var keyColumns = await AssertDbContext.GetTempTableKeyColumns<TempTable<int, string>>().ToListAsync();
         keyColumns.Should().HaveCount(2);
         keyColumns[0].COLUMN_NAME.Should().Be(nameof(TempTable<int, string>.Column1));
         keyColumns[1].COLUMN_NAME.Should().Be(nameof(TempTable<int, string>.Column2));
      }

      [Fact]
      public async Task Should_open_connection()
      {
         await using var con = CreateConnection(TestContext.Instance.ConnectionString);

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options, new DbDefaultSchema(Schema));

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(ctx, ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
      }

      [Fact]
      public async Task Should_return_reference_to_be_able_to_close_connection()
      {
         await using var con = CreateConnection(TestContext.Instance.ConnectionString);

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options, new DbDefaultSchema(Schema));

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await _sut.CreateTempTableAsync(ctx, ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);
         tempTableReference.Dispose();

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_reference_to_be_able_to_close_connection_event_if_ctx_is_disposed()
      {
         await using var con = CreateConnection(TestContext.Instance.ConnectionString);

         var builder = CreateOptionsBuilder(con);

         ITempTableReference tempTableReference;

         await using (var ctx = new TestDbContext(builder.Options, new DbDefaultSchema(Schema)))
         {
            ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

            // ReSharper disable once RedundantArgumentDefaultValue
            tempTableReference = await _sut.CreateTempTableAsync(ctx, ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);
         }

         con.State.Should().Be(ConnectionState.Open);
         tempTableReference.Dispose();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_table_ref_that_does_nothing_after_connection_is_disposed()
      {
         await using var con = CreateConnection(TestContext.Instance.ConnectionString);

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options, new DbDefaultSchema(Schema));

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await _sut.CreateTempTableAsync(ctx, ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);
         con.Dispose();

         con.State.Should().Be(ConnectionState.Closed);
         tempTableReference.Dispose();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_table_ref_that_does_nothing_after_connection_is_closed()
      {
         await using var con = CreateConnection(TestContext.Instance.ConnectionString);

         var builder = CreateOptionsBuilder(con);

         await using var ctx = new TestDbContext(builder.Options, new DbDefaultSchema(Schema));

         ctx.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await _sut.CreateTempTableAsync(ctx, ctx.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);
         con.Close();

         tempTableReference.Dispose();
         con.State.Should().Be(ConnectionState.Closed);
      }

      [Fact]
      public async Task Should_return_reference_to_remove_temp_table()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>();

         // ReSharper disable once RedundantArgumentDefaultValue
         var tempTableReference = await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<CustomTempTable>(), _optionsWithNonUniqueName).ConfigureAwait(false);
         tempTableReference.Dispose();

         var columns = AssertDbContext.GetTempTableColumns<CustomTempTable>().ToList();
         columns.Should().BeEmpty();
      }

      [Fact]
      public async Task Should_create_temp_table_for_entityType()
      {
         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TestEntity>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TestEntity>().OrderBy(c => c.COLUMN_NAME).ToList();
         columns.Should().HaveCount(6);

         ValidateColumn(columns[0], "_privateField", "int", false);
         ValidateColumn(columns[1], nameof(TestEntity.ConvertibleClass), "int", true);
         ValidateColumn(columns[2], nameof(TestEntity.Count), "int", false);
         ValidateColumn(columns[3], nameof(TestEntity.Id), "uniqueidentifier", false);
         ValidateColumn(columns[4], nameof(TestEntity.Name), "nvarchar", true);
         ValidateColumn(columns[5], nameof(TestEntity.PropertyWithBackingField), "int", false);
      }

      [Fact]
      public void Should_throw_if_temp_table_is_not_introduced()
      {
         _sut.Awaiting(async c => await c.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueName).ConfigureAwait(false))
             .Should().Throw<ArgumentException>();
      }

      [Fact]
      public async Task Should_create_temp_table_with_one_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList().Should().HaveCount(1);
      }

      [Fact]
      public async Task Should_create_temp_table_without_primary_key()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int>>().ToListAsync().ConfigureAwait(false);
         constraints.Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_create_temp_table_with_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int?>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", true);
      }

      [Fact]
      public async Task Should_create_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>().Property(t => t.Column1).IsRequired();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int?>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_double()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<double>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<double>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<double>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "float", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<decimal>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>().Property(t => t.Column1).HasColumnType("decimal(20,5)");

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<decimal>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false, 20, 5);
      }

      [Fact]
      public async Task Should_create_temp_table_with_bool()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<bool>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<bool>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<bool>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "bit", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<string>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string_with_max_length()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).HasMaxLength(50);

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<string>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", false, charMaxLength: 50);
      }

      [Fact]
      public async Task Should_create_temp_table_with_2_columns()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

         await _sut.CreateTempTableAsync(ActDbContext, ActDbContext.GetEntityType<TempTable<int, string>>(), _optionsWithNonUniqueName).ConfigureAwait(false);

         var columns = AssertDbContext.GetTempTableColumns<TempTable<int, string>>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "int", false);
         ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "nvarchar", false);
      }

      private void ValidateColumn(InformationSchemaColumn column, string name, string type, bool isNullable, byte? numericPrecision = null, int? numericScale = null, int? charMaxLength = null)
      {
         if (column == null)
            throw new ArgumentNullException(nameof(column));

         column.COLUMN_NAME.Should().Be(name);
         column.DATA_TYPE.Should().Be(type);
         column.IS_NULLABLE.Should().Be(isNullable ? "YES" : "NO");

         if (numericPrecision.HasValue)
            column.NUMERIC_PRECISION.Should().Be(numericPrecision.Value);

         if (numericScale.HasValue)
            column.NUMERIC_SCALE.Should().Be(numericScale.Value);

         if (charMaxLength.HasValue)
            column.CHARACTER_MAXIMUM_LENGTH.Should().Be(charMaxLength.Value);
      }
   }
}
