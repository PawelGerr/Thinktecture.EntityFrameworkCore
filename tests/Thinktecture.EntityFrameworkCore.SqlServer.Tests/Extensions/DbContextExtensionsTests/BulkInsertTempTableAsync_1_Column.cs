using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   public class BulkInsertTempTableAsync_1_Column : IntegrationTestsBase
   {
      public BulkInsertTempTableAsync_1_Column([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var values = new List<int> { 1, 2 };
         var query = await DbContext.BulkInsertTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int>(1), new TempTable<int>(2));
      }

      [Fact]
      public async Task Should_insert_int_with_streaming_disabled()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var options = new SqlBulkInsertOptions { EnableStreaming = false };
         var values = new List<int> { 1, 2 };
         var query = await DbContext.BulkInsertTempTableAsync(values, options).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int>(1), new TempTable<int>(2));
      }

      [Fact]
      public async Task Should_insert_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int?>();

         var values = new List<int?> { 1, null };
         var query = await DbContext.BulkInsertTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<int?>(1), new TempTable<int?>(null));
      }

      [Fact]
      public async Task Should_insert_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string>();

         var values = new List<string> { "value1", null };
         var query = await DbContext.BulkInsertTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(2).And
                  .BeEquivalentTo(new TempTable<string>("value1"), new TempTable<string>(null));
      }
   }
}
