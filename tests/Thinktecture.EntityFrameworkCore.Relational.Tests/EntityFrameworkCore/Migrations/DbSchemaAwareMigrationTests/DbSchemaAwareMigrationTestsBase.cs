using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public class DbSchemaAwareMigrationTestsBase : TestBase
   {
      protected IDbContextSchema Schema { get; set; }

      private TestDbSchemaAwareMigration _sut;

      [NotNull]
      protected TestDbSchemaAwareMigration SUT => _sut ?? (_sut = new TestDbSchemaAwareMigration(Schema));
   }
}
