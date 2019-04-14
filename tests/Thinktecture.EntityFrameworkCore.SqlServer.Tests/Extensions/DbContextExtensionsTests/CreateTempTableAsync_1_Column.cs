using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class CreateTempTableAsync_1_Column : CreateTempTableAsyncBase
   {
      public CreateTempTableAsync_1_Column([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_throw_if_temp_table_is_not_introduced()
      {
         DbContext.Awaiting(async ctx => await ctx.CreateTempTableAsync<int>().ConfigureAwait(false))
                  .Should().Throw<ArgumentException>();
      }

      [Fact]
      public async Task Should_create_temp_table_with_one_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await DbContext.CreateTempTableAsync<int>().ConfigureAwait(false);

         DbContext.GetTempTableColumns<int>().ToList().Should().HaveCount(1);
      }

      [Fact]
      public async Task Should_create_temp_table_without_primary_key()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await DbContext.CreateTempTableAsync<int>().ConfigureAwait(false);

         var constraints = await DbContext.GetTempTableConstraints<int>().ToListAsync().ConfigureAwait(false);
         constraints.Should().HaveCount(0);
      }

      [Fact]
      public async Task Should_create_temp_table_with_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await DbContext.CreateTempTableAsync<int>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<int>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         await DbContext.CreateTempTableAsync<int?>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<int?>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", true);
      }

      [Fact]
      public async Task Should_create_make_nullable_int_to_non_nullable_if_set_via_modelbuilder()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>().Property(t => t.Column1).IsRequired();

         await DbContext.CreateTempTableAsync<int?>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<int?>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<int?>.Column1), "int", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_double()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<double>();

         await DbContext.CreateTempTableAsync<double>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<double>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<double>.Column1), "float", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>();

         await DbContext.CreateTempTableAsync<decimal>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<decimal>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_decimal_with_explicit_precision()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<decimal>().Property(t => t.Column1).HasColumnType("decimal(20,5)");

         await DbContext.CreateTempTableAsync<decimal>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<decimal>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<decimal>.Column1), "decimal", false, 20, 5);
      }

      [Fact]
      public async Task Should_create_temp_table_with_bool()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<bool>();

         await DbContext.CreateTempTableAsync<bool>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<bool>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<bool>.Column1), "bit", false);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         await DbContext.CreateTempTableAsync<string>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<string>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true);
      }

      [Fact]
      public async Task Should_create_temp_table_with_string_with_max_length()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).HasMaxLength(50);

         await DbContext.CreateTempTableAsync<string>().ConfigureAwait(false);

         var columns = DbContext.GetTempTableColumns<string>().ToList();
         ValidateColumn(columns[0], nameof(TempTable<string>.Column1), "nvarchar", true, charMaxLength: 50);
      }
   }
}
