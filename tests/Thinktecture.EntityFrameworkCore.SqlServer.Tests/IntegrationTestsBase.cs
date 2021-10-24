using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture
{
   [SuppressMessage("ReSharper", "EF1001")]
   [Collection("SqlServerTests")]
   public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
   {
      protected Action<ModelBuilder>? ConfigureModel { get; set; }
      protected IReadOnlyCollection<string> SqlStatements { get; }

      protected bool IsTenantDatabaseSupportEnabled { get; set; }
      protected Mock<ITenantDatabaseProvider> TenantDatabaseProviderMock { get; }

      protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, bool useSharedTables)
         : base(TestContext.Instance.ConnectionString, useSharedTables)
      {
         DisableModelCache = true;

         var loggerFactory = TestContext.Instance.GetLoggerFactory(testOutputHelper);
         SqlStatements = loggerFactory.CollectExecutedCommands();

         UseLoggerFactory(loggerFactory);

         TenantDatabaseProviderMock = new Mock<ITenantDatabaseProvider>(MockBehavior.Strict);
      }

      protected IDiagnosticsLogger<TCategory> CreateDiagnosticsLogger<TCategory>(ILoggingOptions? options = null, DiagnosticSource? diagnosticSource = null)
         where TCategory : LoggerCategory<TCategory>, new()
      {
         return new DiagnosticsLogger<TCategory>(LoggerFactory, options ?? new LoggingOptions(),
                                                 diagnosticSource ?? new DiagnosticListener(typeof(TCategory).ShortDisplayName()),
                                                 new SqlServerLoggingDefinitions(),
                                                 new NullDbContextLogger());
      }

      /// <inheritdoc />
      protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options, IDbDefaultSchema schema)
      {
         var ctx = base.CreateContext(options, schema);
         ctx.ConfigureModel = ConfigureModel;

         return ctx;
      }

      /// <inheritdoc />
      protected override DbContextOptionsBuilder<TestDbContext> CreateOptionsBuilder(DbConnection? connection)
      {
         var builder = base.CreateOptionsBuilder(connection)
                           .AddNestedTransactionSupport()
                           .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.Add(ServiceDescriptor.Singleton(TenantDatabaseProviderMock)));

         return builder;
      }

      /// <inheritdoc />
      protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder builder)
      {
         base.ConfigureSqlServer(builder);

         builder.AddBulkOperationSupport()
                .AddRowNumberSupport();

         if (IsTenantDatabaseSupportEnabled)
            builder.AddTenantDatabaseSupport<TestTenantDatabaseProviderFactory>();
      }

      /// <inheritdoc />
      protected override string DetermineSchema(bool useSharedTables)
      {
         return useSharedTables ? $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests" : base.DetermineSchema(false);
      }
   }
}
