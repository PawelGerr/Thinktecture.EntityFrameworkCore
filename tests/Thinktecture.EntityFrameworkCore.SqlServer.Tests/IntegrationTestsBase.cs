using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

namespace Thinktecture
{
   [SuppressMessage("ReSharper", "EF1001")]
   public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
   {
      private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new ConcurrentDictionary<ITestOutputHelper, ILoggerFactory>();

      protected ILoggerFactory LoggerFactory { get; }

      public Action<ModelBuilder>? ConfigureModel { get; set; }

      protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, bool useSharedTables)
         : base(TestContext.Instance.ConnectionString, useSharedTables)
      {
         DisableModelCache = true;
         LoggerFactory = CreateLoggerFactory(testOutputHelper);
         UseLoggerFactory(LoggerFactory);
      }

      protected IDiagnosticsLogger<TCategory> CreateDiagnosticsLogger<TCategory>(ILoggingOptions? options = null, DiagnosticSource? diagnosticSource = null)
         where TCategory : LoggerCategory<TCategory>, new()
      {
         return new DiagnosticsLogger<TCategory>(LoggerFactory, options ?? new LoggingOptions(), diagnosticSource ?? new DiagnosticListener(typeof(TCategory).DisplayName()), new SqlServerLoggingDefinitions());
      }

      /// <inheritdoc />
      protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options, IDbDefaultSchema schema)
      {
         var ctx = base.CreateContext(options, schema);
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

      /// <inheritdoc />
      protected override DbContextOptionsBuilder<TestDbContext> CreateOptionsBuilder(DbConnection connection)
      {
         return base.CreateOptionsBuilder(connection)
                    .AddRowNumberSupport();
      }

      /// <inheritdoc />
      protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder)
      {
         base.ConfigureSqlServer(builder);

         builder.AddTempTableSupport();
      }

      /// <inheritdoc />
      protected override string DetermineSchema(bool useSharedTables)
      {
         return useSharedTables ? $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests" : base.DetermineSchema(false);
      }
   }
}
