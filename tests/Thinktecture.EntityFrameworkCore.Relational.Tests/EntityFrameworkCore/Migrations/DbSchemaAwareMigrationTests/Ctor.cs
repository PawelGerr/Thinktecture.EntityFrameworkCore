using FluentAssertions;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class Ctor : DbSchemaAwareMigrationTestsBase
   {
      [Fact]
      public void Should_set_schema_to_null_if_param_is_null()
      {
         var migration = new TestDbSchemaAwareMigration(null);
         migration.Schema.Should().BeNull();
      }
   }
}
