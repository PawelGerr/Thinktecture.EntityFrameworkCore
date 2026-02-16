using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Diagnostics;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.Json;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

[Collection("NpgsqlTests")]
public class IntegrationTestsBase : IAsyncLifetime, IAsyncDisposable
{
   private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { Converters = { new ConvertibleClassConverter() } };

   private readonly CommandCapturingInterceptor _commandCapturingInterceptor = new();
   private readonly string _connectionString;
   private readonly ITestOutputHelper _testOutputHelper;
   private readonly string _schema;

   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected Action<DbContextOptionsBuilder>? Configure { get; set; }
   protected IReadOnlyCollection<string> ExecutedCommands => _commandCapturingInterceptor.Commands;
   protected string Schema => _schema;

   private TestDbContext? _arrangeDbContext;
   private TestDbContext? _actDbContext;
   private TestDbContext? _assertDbContext;

   protected TestDbContext ArrangeDbContext => _arrangeDbContext ??= CreateDbContext();
   protected TestDbContext ActDbContext => _actDbContext ??= CreateDbContext();
   protected TestDbContext AssertDbContext => _assertDbContext ??= CreateDbContext();

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
   {
      _testOutputHelper = testOutputHelper;
      _connectionString = npgsqlFixture.ConnectionString;

      var gitBranchName = TestContext.Instance.Configuration["SourceBranchName"];
      var schema = String.IsNullOrWhiteSpace(gitBranchName) ? "tests" : $"{gitBranchName}_tests";
      schema += "_" + Environment.Version.Major;

      _schema = schema;
   }

   public async Task InitializeAsync()
   {
      await using var ctx = new TestDbContext(CreateOptions(), new DbDefaultSchema(_schema));

      await ctx.Database.ExecuteSqlRawAsync($"""CREATE SCHEMA IF NOT EXISTS "{_schema}" """);

      await ctx.Database.EnsureCreatedAsync();

      await ctx.Database.ExecuteSqlRawAsync($"""
         DO $$
         DECLARE r RECORD;
         BEGIN
             FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = '{_schema}') LOOP
                 EXECUTE 'TRUNCATE TABLE "{_schema}"."' || r.tablename || '" CASCADE';
             END LOOP;
         END $$;
         """);
   }

   private TestDbContext CreateDbContext()
   {
      var options = CreateOptions();
      var ctx = new TestDbContext(options, new DbDefaultSchema(_schema))
                {
                   ConfigureModel = ConfigureModel,
                   Configure = Configure
                };

      return ctx;
   }

   private DbContextOptions<TestDbContext> CreateOptions()
   {
      var builder = new DbContextOptionsBuilder<TestDbContext>()
         .UseNpgsql(_connectionString, npgsqlOptions => npgsqlOptions.AddBulkOperationSupport()
                                                                        .AddCollectionParameterSupport(_jsonSerializerOptions)
                                                                        .AddWindowFunctionsSupport())
         .AddSchemaRespectingComponents()
         .AddNestedTransactionSupport()
         .AddInterceptors(_commandCapturingInterceptor)
         .UseLoggerFactory(TestContext.Instance.GetLoggerFactory(_testOutputHelper))
         .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning))
         .ReplaceService<IModelCacheKeyFactory, CachePerContextModelCacheKeyFactory>()
         .EnableSensitiveDataLogging();

      return builder.Options;
   }

   async Task IAsyncLifetime.DisposeAsync()
   {
      await DisposeAsync();
   }

   public async ValueTask DisposeAsync()
   {
      if (_arrangeDbContext is not null)
         await _arrangeDbContext.DisposeAsync();

      if (_actDbContext is not null)
         await _actDbContext.DisposeAsync();

      if (_assertDbContext is not null)
         await _assertDbContext.DisposeAsync();
   }
}
