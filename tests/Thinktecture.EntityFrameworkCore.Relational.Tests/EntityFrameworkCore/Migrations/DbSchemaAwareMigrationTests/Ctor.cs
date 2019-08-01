using FluentAssertions;
using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class Ctor : DbSchemaAwareMigrationTestsBase
   {
      public Ctor([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_set_schema_to_null_if_param_is_null()
      {
         var migration = new TestDbSchemaAwareMigration(null);
         migration.Schema.Should().BeNull();
      }
   }
}
