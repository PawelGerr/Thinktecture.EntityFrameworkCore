using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   [Collection("BulkInsertTempTableAsync")]
   public class BulkInsertValuesIntoTempTableAsync_1_Column : IntegrationTestsBase
   {
      public BulkInsertValuesIntoTempTableAsync_1_Column([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var values = new List<int> { 1, 2 };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int>(1), new TempTable<int>(2));
      }

      [Fact]
      public async Task Should_insert_int_with_streaming_disabled()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var options = new SqlTempTableBulkInsertOptions { BulkInsertOptions = { EnableStreaming = false } };
         var values = new List<int> { 1, 2 };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, options).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int>(1), new TempTable<int>(2));
      }

      [Fact]
      public async Task Should_insert_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         var values = new List<int?> { 1, null };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyCreation.None }).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int?>(1), new TempTable<int?>(null));
      }

      [Fact]
      public async Task Should_insert_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         var values = new List<string> { "value1", null };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyCreation.None }).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<string>("value1"), new TempTable<string>(null));
      }

      [Fact]
      public async Task Should_create_pk_by_default_on_string_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>().Property(t => t.Column1).HasMaxLength(100).IsRequired();

         await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<string> { "value" }, new SqlTempTableBulkInsertOptions { TempTableCreationOptions = { MakeTableNameUnique = false } }).ConfigureAwait(false);

         var keys = AssertDbContext.GetTempTableKeyColumns<TempTable<string>>().ToList();
         keys.Should().HaveCount(1);
         keys[0].COLUMN_NAME.Should().Be(nameof(TempTable<string>.Column1));
      }

      [Fact]
      public void Should_throw_when_trying_to_create_pk_on_nullable_column()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         ActDbContext.Awaiting(ctx => ctx.BulkInsertValuesIntoTempTableAsync(new List<int?> { 1 }))
                     .Should().Throw<SqlException>();
      }

      [Fact]
      public async Task Should_create_pk_by_default()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new SqlTempTableBulkInsertOptions { TempTableCreationOptions = { MakeTableNameUnique = false } }).ConfigureAwait(false);

         var keys = ArrangeDbContext.GetTempTableKeyColumns<TempTable<int>>().ToList();
         keys.Should().HaveCount(1);
         keys[0].COLUMN_NAME.Should().Be(nameof(TempTable<int>.Column1));
      }

      [Fact]
      public async Task Should_not_create_pk_if_specified_in_options()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<int> { 1 }, new SqlTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyCreation.None }).ConfigureAwait(false);

         var keys = ArrangeDbContext.GetTempTableKeyColumns<TempTable<int>>().ToList();
         keys.Should().HaveCount(0);
      }
   }
}
