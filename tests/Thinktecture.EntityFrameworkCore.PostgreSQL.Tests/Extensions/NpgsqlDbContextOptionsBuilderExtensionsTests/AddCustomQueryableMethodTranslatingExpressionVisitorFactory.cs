using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.NpgsqlDbContextOptionsBuilderExtensionsTests;

// ReSharper disable InconsistentNaming
[Collection("NpgsqlTests")]
public class AddCustomQueryableMethodTranslatingExpressionVisitorFactory : IAsyncLifetime, IAsyncDisposable
{
   private readonly string _connectionString;
   private readonly ITestOutputHelper _testOutputHelper;
   private readonly string _schema;

   private TestDbContext? _arrangeDbContext;
   private TestDbContext? _actDbContext;

   private TestDbContext ArrangeDbContext => _arrangeDbContext ??= CreateDbContext();
   private TestDbContext ActDbContext => _actDbContext ??= CreateDbContext();

   public AddCustomQueryableMethodTranslatingExpressionVisitorFactory(
      ITestOutputHelper testOutputHelper,
      NpgsqlFixture npgsqlFixture)
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

   [Fact]
   public void Should_enable_AsSubQuery_translation()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new { e.Id, e.Name })
                               .AsSubQuery()
                               .Where(e => e.Name == "1")
                               .ToList();

      result.Should().HaveCount(1);
      result[0].Name.Should().Be("1");
   }

   private TestDbContext CreateDbContext()
   {
      return new TestDbContext(CreateOptions(), new DbDefaultSchema(_schema));
   }

   private DbContextOptions<TestDbContext> CreateOptions()
   {
      var builder = new DbContextOptionsBuilder<TestDbContext>()
         .UseNpgsql(_connectionString, npgsqlOptions => npgsqlOptions.AddCustomQueryableMethodTranslatingExpressionVisitorFactory())
         .AddSchemaRespectingComponents()
         .UseLoggerFactory(TestContext.Instance.GetLoggerFactory(_testOutputHelper))
         .ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)
                                                              .Ignore(RelationalEventId.PendingModelChangesWarning))
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
   }
}
