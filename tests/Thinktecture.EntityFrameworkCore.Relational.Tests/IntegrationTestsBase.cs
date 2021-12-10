using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

public class IntegrationTestsBase : SqliteDbContextIntegrationTests<DbContextWithSchema>
{
   private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new();

   protected Action<DbContextOptionsBuilder<DbContextWithSchema>>? ConfigureOptionsBuilder { get; set; }
   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected string? Schema { get; set; }

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper,
                                  IMigrationExecutionStrategy? migrationExecutionStrategy = null)
      : base(migrationExecutionStrategy ?? MigrationExecutionStrategies.NoMigration)
   {
      DisableModelCache = true;

      var loggerFactory = CreateLoggerFactory(testOutputHelper);
      UseLoggerFactory(loggerFactory);
   }

   protected override DbContextOptionsBuilder<DbContextWithSchema> CreateOptionsBuilder(DbConnection? connection)
   {
      var builder = base.CreateOptionsBuilder(connection);
      ConfigureOptionsBuilder?.Invoke(builder);

      return builder;
   }

   protected override DbContextWithSchema CreateContext(DbContextOptions<DbContextWithSchema> options)
   {
      return new(options, Schema) { ConfigureModel = ConfigureModel };
   }

   private static ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper)
   {
      ArgumentNullException.ThrowIfNull(testOutputHelper);

      return _loggerFactoryCache.GetOrAdd(testOutputHelper, _ =>
                                                            {
                                                               var loggerConfig = new LoggerConfiguration()
                                                                                  .WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

                                                               return new LoggerFactory()
                                                                  .AddSerilog(loggerConfig.CreateLogger());
                                                            });
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
