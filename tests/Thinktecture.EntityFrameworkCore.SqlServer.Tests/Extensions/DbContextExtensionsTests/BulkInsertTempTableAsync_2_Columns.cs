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
   public class BulkInsertTempTableAsync_2_Column : IntegrationTestsBase
   {
      public BulkInsertTempTableAsync_2_Column([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_int_nullable_int()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int, int?>();

         var values = new List<(int, int?)> { (1, null) };
         var query = await DbContext.BulkInsertTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TempTable<int, int?>(1, null));
      }

      [Fact]
      public async Task Should_insert_string_string()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<string, string>();

         var values = new List<(string, string)> { ("value1", null) };
         var query = await DbContext.BulkInsertTempTableAsync(values).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TempTable<string, string>("value1", null));
      }
   }
}
