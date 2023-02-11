using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

public class IntegrationTestsBase : SqliteDbContextIntegrationTests<DbContextWithSchema>
{
   private readonly IMigrationExecutionStrategy? _migrationExecutionStrategy;

   protected Action<DbContextOptionsBuilder<DbContextWithSchema>>? ConfigureOptionsBuilder { get; set; }
   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected string? Schema { get; set; }
   protected ILoggerFactory LoggerFactory { get; }

   protected IntegrationTestsBase(
      ITestOutputHelper testOutputHelper,
      IMigrationExecutionStrategy? migrationExecutionStrategy = null)
      : base(testOutputHelper)
   {
      _migrationExecutionStrategy = migrationExecutionStrategy;
      LoggerFactory = testOutputHelper.ToLoggerFactory();
   }

   protected override void ConfigureTestDbContextProvider(SqliteTestDbContextProviderBuilder<DbContextWithSchema> builder)
   {
      builder.UseMigrationExecutionStrategy(_migrationExecutionStrategy ?? IMigrationExecutionStrategy.NoMigration)
             .UseMigrationLogLevel(LogLevel.Warning)
             .ConfigureOptions(optionsBuilder => ConfigureOptionsBuilder?.Invoke(optionsBuilder))
             .InitializeContext(ctx => ctx.ConfigureModel = ConfigureModel)
             .UseContextFactory(options => new DbContextWithSchema(options, Schema));

      if (ConfigureModel is not null || Schema is not null)
         builder.DisableModelCache();
   }

   protected DbContextWithSchema CreateContextWithSchema(string schema)
   {
      var options = new DbContextOptionsBuilder<DbContextWithSchema>().Options;
      return new DbContextWithSchema(options, schema) { ConfigureModel = ConfigureModel };
   }

   protected DbContextWithoutSchema CreateContextWithoutSchema()
   {
      var options = new DbContextOptionsBuilder<DbContextWithoutSchema>().Options;
      return new DbContextWithoutSchema(options) { ConfigureModel = ConfigureModel };
   }
}
