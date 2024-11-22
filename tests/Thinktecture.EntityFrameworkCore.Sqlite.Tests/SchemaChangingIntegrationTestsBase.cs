using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

public abstract class SchemaChangingIntegrationTestsBase : SqliteDbContextIntegrationTests<TestDbContext>
{
   private readonly IMigrationExecutionStrategy? _migrationExecutionStrategy;

   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected Action<DbContextOptionsBuilder>? Configure { get; set; }
   protected ILoggerFactory LoggerFactory { get; }

   protected SchemaChangingIntegrationTestsBase(
      ITestOutputHelper testOutputHelper,
      IMigrationExecutionStrategy? migrationExecutionStrategy = null)
      : base(testOutputHelper)
   {
      _migrationExecutionStrategy = migrationExecutionStrategy;
      LoggerFactory = testOutputHelper.ToLoggerFactory();
   }

   protected override void ConfigureTestDbContextProvider(SqliteTestDbContextProviderBuilder<TestDbContext> builder)
   {
      builder.UseMigrationExecutionStrategy(_migrationExecutionStrategy ?? IMigrationExecutionStrategy.Migrations)
             .UseMigrationLogLevel(LogLevel.Warning)
             .ConfigureSqliteOptions(optionsBuilder => optionsBuilder.AddBulkOperationSupport()
                                                                     .AddWindowFunctionsSupport())
             .InitializeContext(ctx =>
                                {
                                   ctx.ConfigureModel = ConfigureModel;
                                   ctx.Configure = Configure;
                                });

      if (ConfigureModel is not null)
         builder.DisableModelCache();
   }
}
