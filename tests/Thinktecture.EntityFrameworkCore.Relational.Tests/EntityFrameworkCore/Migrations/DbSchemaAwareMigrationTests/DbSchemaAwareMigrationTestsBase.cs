using JetBrains.Annotations;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class DbSchemaAwareMigrationTestsBase : TestBase
   {
      protected new IDbContextSchema Schema { get; set; }

      private TestDbSchemaAwareMigration _sut;

      [NotNull]
      protected TestDbSchemaAwareMigration SUT => _sut ?? (_sut = new TestDbSchemaAwareMigration(Schema));

      public DbSchemaAwareMigrationTestsBase([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }
   }
}
