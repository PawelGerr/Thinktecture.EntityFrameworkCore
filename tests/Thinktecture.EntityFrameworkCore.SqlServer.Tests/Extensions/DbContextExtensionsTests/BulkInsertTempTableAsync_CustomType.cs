using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertTempTableAsync_CustomType : IntegrationTestsBase
   {
      public BulkInsertTempTableAsync_CustomType([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_customType()
      {
         ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>().Property(t => t.Column2).HasMaxLength(100).IsRequired();

         var entities = new List<CustomTempTable> { new CustomTempTable(1, "value") };
         var query = await DbContext.BulkInsertCustomTempTableAsync<CustomTempTable, int, string>(entities).ConfigureAwait(false);

         var tempTable = await query.ToListAsync().ConfigureAwait(false);
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new CustomTempTable(1, "value"));
      }
   }
}
