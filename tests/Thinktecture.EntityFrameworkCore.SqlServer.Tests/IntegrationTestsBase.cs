using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.Json;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

[Collection("SqlServerTests")]
public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
{
   private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { Converters = { new ConvertibleClassConverter() } };

   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected IReadOnlyCollection<string> ExecutedCommands => TestCtxProvider.ExecutedCommands ?? throw new InvalidOperationException("Capturing executed commands wasn't enabled.");
   protected string Schema => TestCtxProvider.Schema;

   protected bool IsTenantDatabaseSupportEnabled { get; set; }
   protected Mock<ITenantDatabaseProvider> TenantDatabaseProviderMock { get; }

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, bool useSharedTables)
      : base(TestContext.Instance.ConnectionString, useSharedTables, testOutputHelper)
   {
      TenantDatabaseProviderMock = new Mock<ITenantDatabaseProvider>(MockBehavior.Strict);
   }

   protected override void ConfigureTestDbContextProvider(SqlServerTestDbContextProviderBuilder<TestDbContext> builder)
   {
      var gitBranchName = TestContext.Instance.Configuration["SourceBranchName"];
      var schema = String.IsNullOrWhiteSpace(gitBranchName) ? "tests" : $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests";

      builder.UseMigrationExecutionStrategy(IMigrationExecutionStrategy.Migrations)
             .UseMigrationLogLevel(LogLevel.Warning)
             .CollectExecutedCommands()
             .ConfigureOptions((optionsBuilder, _) =>
                               {
                                  optionsBuilder.AddNestedTransactionSupport()
                                                .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

                                  optionsBuilder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.Register(typeof(Mock<ITenantDatabaseProvider>), TenantDatabaseProviderMock));
                               })
             .ConfigureSqlServerOptions((optionsBuilder, _) =>
                                        {
                                           optionsBuilder.AddBulkOperationSupport()
                                                         .AddRowNumberSupport()
                                                         .AddCollectionParameterSupport(_jsonSerializerOptions);

                                           if (IsTenantDatabaseSupportEnabled)
                                              optionsBuilder.AddTenantDatabaseSupport<TestTenantDatabaseProviderFactory>();
                                        })
             .UseSharedTableSchema(schema)
             .InitializeContext(ctx => ctx.ConfigureModel = ConfigureModel)
             .DisableModelCache();
   }
}
