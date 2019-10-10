using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Diagnostics.Internal;
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
   public class IntegrationTestsBase : SqliteDbContextIntegrationTests<TestDbContext>
   {
      private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new ConcurrentDictionary<ITestOutputHelper, ILoggerFactory>();

      protected ILoggerFactory LoggerFactory { get; }
      protected Action<DbContextOptionsBuilder<TestDbContext>>? ConfigureOptionsBuilder { get; set; }
      protected Action<ModelBuilder>? ConfigureModel { get; set; }

      protected IntegrationTestsBase(ITestOutputHelper testOutputHelper,
                                     IMigrationExecutionStrategy? migrationExecutionStrategy = null)
         : base(migrationExecutionStrategy)
      {
         DisableModelCache = true;

         LoggerFactory = CreateLoggerFactory(testOutputHelper);
         UseLoggerFactory(LoggerFactory);
      }

      protected IDiagnosticsLogger<TCategory> CreateDiagnosticsLogger<TCategory>(ILoggingOptions? options = null, DiagnosticSource? diagnosticSource = null)
         where TCategory : LoggerCategory<TCategory>, new()
      {
         return new DiagnosticsLogger<TCategory>(LoggerFactory, options ?? new LoggingOptions(), diagnosticSource ?? new DiagnosticListener(typeof(TCategory).DisplayName()), new SqliteLoggingDefinitions());
      }

      protected override DbContextOptionsBuilder<TestDbContext> CreateOptionsBuilder(DbConnection connection)
      {
         var builder = base.CreateOptionsBuilder(connection);
         ConfigureOptionsBuilder?.Invoke(builder);

         return builder;
      }

      /// <inheritdoc />
      protected override void ConfigureSqlite(SqliteDbContextOptionsBuilder builder)
      {
         base.ConfigureSqlite(builder);

         builder.AddTempTableSupport();
      }

      protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options)
      {
         var ctx = base.CreateContext(options);
         ctx.ConfigureModel = ConfigureModel;

         return ctx;
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
   }
}
