using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.Diagnostics;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore;

[CollectionDefinition(NAME, DisableParallelization = true)]
public sealed class MySqlTestCollection : ICollectionFixture<MySqlContainerFixture>
{
   public const string NAME = "MySqlTests";
}

public abstract class MySqlIntegrationTestsBase : IAsyncLifetime, IAsyncDisposable
{
   private readonly ITestOutputHelper _testOutputHelper;
   private readonly MySqlContainerFixture _fixture;
   private readonly CommandCapturingInterceptor _commandCapturingInterceptor = new();
   private MySqlTestDbContext? _arrangeDbContext;
   private MySqlTestDbContext? _actDbContext;
   private MySqlTestDbContext? _assertDbContext;

   protected MySqlIntegrationTestsBase(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
   {
      _testOutputHelper = testOutputHelper;
      _fixture = fixture;
   }

   protected string ConnectionString => _fixture.ConnectionString;
   protected Action<ModelBuilder>? ConfigureModel { get; set; }
   protected Action<DbContextOptionsBuilder>? Configure { get; set; }

   /// <summary>
   /// Configures the MySQL specific options (for example <c>AddCollectionParameterSupport()</c>) of the Arrange/Act/Assert contexts.
   /// Has to be set before the first context is accessed.
   /// </summary>
   protected Action<MySqlDbContextOptionsBuilder>? ConfigureMySql { get; set; }

   /// <summary>
   /// Registers the bulk copy insert path (<c>AddBulkOperationSupport(useBulkCopy: true)</c>) for the Arrange/Act/Assert contexts
   /// and allows <c>LOAD DATA LOCAL INFILE</c> on their connections (the server is started with <c>--local-infile=1</c>).
   /// Has to be set before the first context is accessed.
   /// </summary>
   protected bool UseBulkCopy { get; set; }

   protected IReadOnlyCollection<string> ExecutedCommands => _commandCapturingInterceptor.Commands;
   protected MySqlTestDbContext ArrangeDbContext => _arrangeDbContext ??= CreateDbContext();
   protected MySqlTestDbContext ActDbContext => _actDbContext ??= CreateDbContext();
   protected MySqlTestDbContext AssertDbContext => _assertDbContext ??= CreateDbContext();

   public async Task InitializeAsync()
   {
      await using var context = CreateDbContext();

      await context.Database.EnsureCreatedAsync();
      await TruncateNonEmptyTablesAsync(context);
   }

   // Every test starts with empty tables, whatever entities it uses. Only tables that contain rows are truncated (TRUNCATE is DDL and slow),
   // which also resets AUTO_INCREMENT counters.
   private static async Task TruncateNonEmptyTablesAsync(MySqlTestDbContext context)
   {
      var tables = await context.Database
                                .SqlQueryRaw<string>("SELECT TABLE_NAME AS `Value` FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'")
                                .ToListAsync();

      if (tables.Count == 0)
         return;

      var probe = String.Join(" UNION ALL ", tables.Select(table => $"(SELECT '{Escape(table, '\'')}' AS `Value` FROM `{Escape(table, '`')}` LIMIT 1)"));
      var nonEmptyTables = await context.Database.SqlQueryRaw<string>(probe).ToListAsync();

      if (nonEmptyTables.Count == 0)
         return;

      var statements = String.Concat(nonEmptyTables.Select(table => $"TRUNCATE TABLE `{Escape(table, '`')}`;"));

      // The setting is session scoped; it is switched back on explicitly because the connection returns to the pool.
      await context.Database.ExecuteSqlRawAsync($"SET FOREIGN_KEY_CHECKS = 0;{statements}SET FOREIGN_KEY_CHECKS = 1;");
   }

   private static string Escape(string value, char quote)
   {
      return value.Replace(quote.ToString(), new string(quote, 2), StringComparison.Ordinal);
   }

   protected Task<MySqlTestDbContext> CreateContextAsync(Action<MySqlDbContextOptionsBuilder>? configure = null)
   {
      return Task.FromResult(CreateDbContext(configure));
   }

   private MySqlTestDbContext CreateDbContext(Action<MySqlDbContextOptionsBuilder>? configure = null)
   {
      return new MySqlTestDbContext(CreateOptions(configure))
             {
                Configure = Configure,
                ConfigureModel = ConfigureModel
             };
   }

   private DbContextOptions<MySqlTestDbContext> CreateOptions(Action<MySqlDbContextOptionsBuilder>? configure)
   {
      var builder = MySqlTestDbContext.CreateOptionsBuilder()
                    .UseMySql(GetConnectionString(), MySqlContainerFixture.ServerVersion, mySqlBuilder =>
                                                                                              {
                                                                                                 mySqlBuilder.AddBulkOperationSupport(useBulkCopy: UseBulkCopy).AddWindowFunctionsSupport();
                                                                                                 ConfigureMySql?.Invoke(mySqlBuilder);
                                                                                                 configure?.Invoke(mySqlBuilder);
                                                                                              })
                    .AddNestedTransactionSupport()
                    .AddInterceptors(_commandCapturingInterceptor)
                    .UseLoggerFactory(TestContext.Instance.GetLoggerFactory(_testOutputHelper))
                    .EnableSensitiveDataLogging();

      // EF caches the model per context type; a per-test model configuration must not leak into other tests.
      if (ConfigureModel is not null)
         builder.ReplaceService<IModelCacheKeyFactory, CachePerContextModelCacheKeyFactory>();

      return builder.Options;
   }

   private string GetConnectionString()
   {
      return UseBulkCopy
                ? new MySqlConnectionStringBuilder(_fixture.ConnectionString) { AllowLoadLocalInfile = true }.ConnectionString
                : _fixture.ConnectionString;
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

   async Task IAsyncLifetime.DisposeAsync()
   {
      await DisposeAsync();
   }
}
