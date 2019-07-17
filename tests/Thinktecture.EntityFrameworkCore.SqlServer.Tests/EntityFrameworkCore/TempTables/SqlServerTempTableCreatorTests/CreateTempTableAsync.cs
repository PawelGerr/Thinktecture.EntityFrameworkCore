using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests
{
   // ReSharper disable once InconsistentNaming
   public class CreateTempTableAsync : IntegrationTestsBase
   {
      private readonly SqlServerTempTableCreator _sut = new SqlServerTempTableCreator();

      public CreateTempTableAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_create_temp_table_for_queryType()
      {
         ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<CustomTempTable>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetCustomTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
      }

      [Fact]
      public async Task Should_create_temp_table_for_entityType()
      {
         // ReSharper disable once RedundantArgumentDefaultValue
         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TestEntity>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetCustomTempTableColumns<TestEntity>().OrderBy(c => c.COLUMN_NAME).ToList();
         columns.Should().HaveCount(6);

         ValidateColumn(columns[0], "_privateField", "int", false);
         ValidateColumn(columns[1], nameof(TestEntity.Count), "int", false);
         ValidateColumn(columns[2], nameof(TestEntity.Id), "uniqueidentifier", false);
         ValidateColumn(columns[3], nameof(TestEntity.Name), "nvarchar", true);
         ValidateColumn(columns[4], nameof(TestEntity.PropertyWithBackingField), "int", false);
         ValidateColumn(columns[5], "ShadowProperty", "nvarchar", true);
      }

      [Fact]
      public void Should_throw_if_temp_table_is_not_introduced()
      {
         _sut.Awaiting(async c => await c.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false))
             .Should().Throw<ArgumentException>();
      }

      [Fact]
      public async Task Should_create_temp_table_with_one_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         DbContext.GetTempTableColumns<TempTable<int>>().ToList().Should().HaveCount(1);
      }

      [Fact]
      public async Task Should_create_temp_table_without_primary_key()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var constraints = await DbContext.GetTempTableConstraints<TempTable<int>>().ToListAsync().ConfigureAwait(false);
         constraints.Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_create_temp_table_with_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<int>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int?>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", true);
      }

      [Fact]
      public async Task Should_create_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>().Property(t => t.Column1).IsRequired();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int?>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<int?>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_double()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<double>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<double>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<double>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "float", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<decimal>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>().Property(t => t.Column1).HasColumnType("decimal(20,5)");

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<decimal>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<decimal>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false, 20, 5);
      }

      [Fact]
      public async Task Should_create_temp_table_with_bool()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<bool>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<bool>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<bool>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "bit", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<string>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string_with_max_length()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).HasMaxLength(50);

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<string>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<string>>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true, charMaxLength: 50);
      }

      [Fact]
      public async Task Should_create_temp_table_with_2_columns()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, string>();

         await _sut.CreateTempTableAsync(DbContext, DbContext.GetEntityType<TempTable<int, string>>(), new TempTableCreationOptions { MakeTableNameUnique = false }).ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<TempTable<int, string>>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(TempTable<int, string>.Column1), "int", false);
         ValidateColumn(columns[1], nameof(TempTable<int, string>.Column2), "nvarchar", true);
      }

      private void ValidateColumn([NotNull] InformationSchemaColumn column, string name, string type, bool isNullable, byte? numericPrecision = null, int? numericScale = null, int? charMaxLength = null)
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
