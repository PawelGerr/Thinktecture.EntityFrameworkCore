using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   public class CreateTempTableAsync : CreateTempTableAsyncBase
   {
      public CreateTempTableAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public async Task Should_create_temp_table_for_queryType()
      {
         ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>();

         await DbContext.CreateTempTableAsync<CustomTempTable>().ConfigureAwait(false);

         var columns = DbContext.GetCustomTempTableColumns<CustomTempTable>().ToList();
         columns.Should().HaveCount(2);

         ValidateColumn(columns[0], nameof(CustomTempTable.Column1), "int", false);
         ValidateColumn(columns[1], nameof(CustomTempTable.Column2), "nvarchar", true);
      }

      [Fact]
      public async Task Should_create_temp_table_for_entityType()
      {
         await DbContext.CreateTempTableAsync<TestEntity>().ConfigureAwait(false);

         var columns = DbContext.GetCustomTempTableColumns<TestEntity>().OrderBy(c => c.COLUMN_NAME).ToList();
         columns.Should().HaveCount(3);

         ValidateColumn(columns[0], nameof(TestEntity.Count), "int", false);
         ValidateColumn(columns[1], nameof(TestEntity.Id), "uniqueidentifier", false);
         ValidateColumn(columns[2], nameof(TestEntity.Name), "nvarchar", true);
      }
   }
}
