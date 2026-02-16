using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

[Collection("NpgsqlTests")]
public abstract class ExistTestBase
{
   protected MigrationExtensionsTestDbContext Context { get; }
   protected DelegatingMigration Migration { get; }
   protected Action ExecuteMigration { get; }

   protected ExistTestBase(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
   {
      var loggerFactory = TestContext.Instance.GetLoggerFactory(testOutputHelper);
      var options = new DbContextOptionsBuilder<MigrationExtensionsTestDbContext>()
                    .UseNpgsql(npgsqlFixture.ConnectionString,
                               npgsqlBuilder => npgsqlBuilder.UseThinktectureNpgsqlMigrationsSqlGenerator())
                    .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
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
