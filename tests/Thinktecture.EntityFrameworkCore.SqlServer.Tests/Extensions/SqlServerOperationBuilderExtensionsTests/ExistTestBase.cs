using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.Extensions.SqlServerOperationBuilderExtensionsTests;

[Collection("SqlServerTests")]
public abstract class ExistTestBase
{
   protected MigrationExtensionsTestDbContext Context { get; }
   protected DelegatingMigration Migration { get; }
   protected Action ExecuteMigration { get; }

   protected ExistTestBase(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
   {
      var loggerFactory = TestContext.Instance.GetLoggerFactory(testOutputHelper);
      var options = new DbContextOptionsBuilder<MigrationExtensionsTestDbContext>()
                    .UseSqlServer(sqlServerFixture.ConnectionString,
                                  sqlServerBuilder => sqlServerBuilder.UseThinktectureSqlServerMigrationsSqlGenerator())
                    .UseLoggerFactory(loggerFactory)
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .Options;

      Context = new MigrationExtensionsTestDbContext(options);
      Migration = new DelegatingMigration();

      Context.Database.OpenConnection();

      var migrator = Context.GetService<IMigrationsSqlGenerator>();
      var commandExecutor = Context.GetService<IMigrationCommandExecutor>();
      var connection = Context.GetService<IRelationalConnection>();

      ExecuteMigration = () =>
                         {
                            var commands = migrator.Generate(Migration.UpOperations);
                            commandExecutor.ExecuteNonQuery(commands, connection);
                         };
   }
}
