using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   [Collection("BulkInsertTempTableAsync")]
   public class BulkInsertValuesIntoTempTableAsync_2_Column : IntegrationTestsBase
   {
      public BulkInsertValuesIntoTempTableAsync_2_Column(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_int_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var values = new List<(int, int?)> { (1, null) };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyCreation.None }).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TempTable<int, int?>(1, null));
      }

      [Fact]
      public async Task Should_insert_string_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string, string>().Property(t => t.Column2).IsRequired(false);

         var values = new List<(string, string?)> { ("value1", null) };
         var query = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values, new SqlTempTableBulkInsertOptions { PrimaryKeyCreation = PrimaryKeyCreation.None }).ConfigureAwait(false);

         var tempTable = await query.Query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TempTable<string, string?>("value1", null));
      }

      [Fact]
      public async Task Should_create_pk_by_default()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int>();

         await ActDbContext.BulkInsertValuesIntoTempTableAsync(new List<(int, int)> { (1, 2) }, new SqlTempTableBulkInsertOptions { TempTableCreationOptions = { MakeTableNameUnique = false } }).ConfigureAwait(false);

         var keys = AssertDbContext.GetTempTableKeyColumns<int, int>().ToList();
         keys.Should().HaveCount(2);
         keys[0].COLUMN_NAME.Should().Be(nameof(TempTable<int, int>.Column1));
         keys[1].COLUMN_NAME.Should().Be(nameof(TempTable<int, int>.Column2));
      }
   }
}
