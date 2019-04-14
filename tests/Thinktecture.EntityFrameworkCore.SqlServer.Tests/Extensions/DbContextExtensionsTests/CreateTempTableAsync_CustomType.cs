using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   public class CreateTempTableAsync_CustomType : CreateTempTableAsyncBase
   {
      public CreateTempTableAsync_CustomType([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public async Task Should_create_temp_table_with_custom_type()
      {
         ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         await DbContext.CreateCustomTempTableAsync<CustomTempTable>().ConfigureAwait(false);

         var columns = DbContext.GetCustomTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
      }
   }
}
