using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

[assembly: SuppressMessage("ReSharper", "CA1063")]
[assembly: SuppressMessage("ReSharper", "CA1816")]
[assembly: SuppressMessage("ReSharper", "CA1822")]

namespace Thinktecture
{
   public class IntegrationTestsBase : SqliteDbContextIntegrationTests<DbContextWithSchema>
   {
      private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new ConcurrentDictionary<ITestOutputHelper, ILoggerFactory>();

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

      protected override DbContextOptionsBuilder<DbContextWithSchema> CreateOptionsBuilder(DbConnection connection)
      {
         var builder = base.CreateOptionsBuilder(connection);
         ConfigureOptionsBuilder?.Invoke(builder);

         return builder;
      }

      protected override DbContextWithSchema CreateContext(DbContextOptions<DbContextWithSchema> options)
      {
         return new DbContextWithSchema(options, Schema) { ConfigureModel = ConfigureModel };
      }

      private ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper)
      {
         if (testOutputHelper == null)
            throw new ArgumentNullException(nameof(testOutputHelper));

         return _loggerFactoryCache.GetOrAdd(testOutputHelper, helper =>
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
}
