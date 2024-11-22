using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
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
   protected Action<DbContextOptionsBuilder>? Configure { get; set; }
   protected IReadOnlyCollection<string> ExecutedCommands => TestCtxProvider.ExecutedCommands ?? throw new InvalidOperationException("Capturing executed commands wasn't enabled.");
   protected string? Schema => TestCtxProvider.Schema;

   protected bool IsTenantDatabaseSupportEnabled { get; set; }
   protected ITenantDatabaseProvider TenantDatabaseProviderMock { get; }

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : this(sqlServerFixture.ConnectionString, testOutputHelper, ITestIsolationOptions.DeleteData(NonExistingTableFilter))
   {
      Configure = b => b.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));
   }

   private static bool NonExistingTableFilter(IEntityType entityType)
   {
      return entityType.ClrType != typeof(TestEntityWithCollation)
             && entityType.ClrType != typeof(CustomTempTable)
             && entityType.ClrType != typeof(OwnedEntity)
             && entityType.ClrType != typeof(OwnedEntity_Owns_Inline)
             && entityType.ClrType != typeof(OwnedEntity_Owns_SeparateOne)
             && entityType.ClrType != typeof(OwnedEntity_Owns_SeparateMany)
             && entityType.ClrType != typeof(MyParameter)
             && entityType.ClrType != typeof(TestTemporalTableEntity);
   }

   protected IntegrationTestsBase(string connectionString, ITestOutputHelper testOutputHelper, ITestIsolationOptions isolationOptions)
      : base(connectionString, isolationOptions, testOutputHelper)
   {
      TenantDatabaseProviderMock =  Substitute.For<ITenantDatabaseProvider>();
   }

   protected override void ConfigureTestDbContextProvider(SqlServerTestDbContextProviderBuilder<TestDbContext> builder)
   {
      var gitBranchName = TestContext.Instance.Configuration["SourceBranchName"];
      var schema = String.IsNullOrWhiteSpace(gitBranchName) ? "tests" : $"{TestContext.Instance.Configuration["SourceBranchName"]}_tests";

      schema += "_" + Environment.Version.Major; // for multi-targeting

      builder.UseMigrationExecutionStrategy(new OneTimeMigrationStrategy())
             .UseMigrationLogLevel(LogLevel.Warning)
             .CollectExecutedCommands()
             .ConfigureOptions((optionsBuilder, _) =>
                               {
                                  optionsBuilder.AddNestedTransactionSupport()
                                                .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

                                  optionsBuilder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension =>
                                                                                                           {
                                                                                                              extension.Register(typeof(ITenantDatabaseProvider), TenantDatabaseProviderMock);
                                                                                                              return extension;
                                                                                                           });
                               })
             .ConfigureSqlServerOptions((optionsBuilder, _) =>
                                        {
                                           optionsBuilder.AddBulkOperationSupport()
                                                         .AddWindowFunctionsSupport()
                                                         .AddCollectionParameterSupport(_jsonSerializerOptions);

                                           if (IsTenantDatabaseSupportEnabled)
                                              optionsBuilder.AddTenantDatabaseSupport<TestTenantDatabaseProviderFactory>();
                                        })
             .UseSharedTableSchema(schema)
             .InitializeContext(ctx =>
                                {
                                   ctx.ConfigureModel = ConfigureModel;
                                   ctx.Configure = Configure;
                                });
   }
}
