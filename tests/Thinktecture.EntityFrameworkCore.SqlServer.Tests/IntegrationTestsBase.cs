using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

namespace Thinktecture
{
   public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
   {
      private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new ConcurrentDictionary<ITestOutputHelper, ILoggerFactory>();

      protected ILoggerFactory LoggerFactory { get; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      protected IntegrationTestsBase([NotNull] ITestOutputHelper testOutputHelper, bool useSharedTables)
         : base(TestContext.Instance.ConnectionString, useSharedTables)
      {
         LoggerFactory = CreateLoggerFactory(testOutputHelper);
         UseLoggerFactory(LoggerFactory);
      }

      [NotNull]
      protected IDiagnosticsLogger<TCategory> CreateDiagnosticsLogger<TCategory>([CanBeNull] ILoggingOptions options = null, [CanBeNull] DiagnosticSource diagnosticSource = null)
         where TCategory : LoggerCategory<TCategory>, new()
      {
         return new DiagnosticsLogger<TCategory>(LoggerFactory, options ?? new LoggingOptions(), diagnosticSource ?? new DiagnosticListener(typeof(TCategory).DisplayName()));
      }

      /// <inheritdoc />
      protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options, IDbContextSchema schema)
      {
         var ctx = base.CreateContext(options, schema);
         ctx.ConfigureModel = ConfigureModel;

         return ctx;
      }

      private ILoggerFactory CreateLoggerFactory([NotNull] ITestOutputHelper testOutputHelper)
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

      /// <inheritdoc />
      protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder)
      {
         base.ConfigureSqlServer(builder);

         builder.AddRowNumberSupport()
                .AddTempTableSupport();
      }

      /// <inheritdoc />
      protected override string DetermineSchema(bool useSharedTables)
      {
         return useSharedTables ? $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests" : base.DetermineSchema(false);
      }
   }
}
