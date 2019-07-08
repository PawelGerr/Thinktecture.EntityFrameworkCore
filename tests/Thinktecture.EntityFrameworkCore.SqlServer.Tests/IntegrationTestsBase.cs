using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit.Abstractions;

namespace Thinktecture
{
   public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
   {
      protected LoggingLevelSwitch LogLevelSwitch { get; }

      public Action<ModelBuilder> ConfigureModel { get; set; }

      protected IntegrationTestsBase([NotNull] ITestOutputHelper testOutputHelper, bool useSharedTables)
         : base(TestContext.Instance.ConnectionString, useSharedTables)
      {
         LogLevelSwitch = new LoggingLevelSwitch();
         var loggerFactory = CreateLoggerFactory(testOutputHelper, LogLevelSwitch);
         UseLoggerFactory(loggerFactory);
      }

      /// <inheritdoc />
      protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options, IDbContextSchema schema)
      {
         return new TestDbContext(options, schema) { ConfigureModel = ConfigureModel };
      }

      private ILoggerFactory CreateLoggerFactory([NotNull] ITestOutputHelper testOutputHelper, LoggingLevelSwitch loggingLevelSwitch)
      {
         if (testOutputHelper == null)
            throw new ArgumentNullException(nameof(testOutputHelper));

         var loggerConfig = new LoggerConfiguration()
                            .MinimumLevel.ControlledBy(loggingLevelSwitch)
                            .WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

         return new LoggerFactory()
            .AddSerilog(loggerConfig.CreateLogger());
      }

      /// <inheritdoc />
      protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder)
      {
         base.ConfigureSqlServer(builder);

         builder.AddRowNumberSupport()
                .AddTempTableSupport();
      }

      /// <inheritdoc />
      protected override string GetSchema(bool useSharedTables)
      {
         return useSharedTables ? $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests" : base.GetSchema(false);
      }
   }
}
