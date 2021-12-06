using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Diagnostics.Internal;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

[assembly: SuppressMessage("ReSharper", "CA1063")]
[assembly: SuppressMessage("ReSharper", "CA1816")]
[assembly: SuppressMessage("ReSharper", "CA1822")]

namespace Thinktecture;

public class IntegrationTestsBase : SqliteDbContextIntegrationTests<TestDbContext>
{
   private static readonly ConcurrentDictionary<ITestOutputHelper, ILoggerFactory> _loggerFactoryCache = new();

   protected Action<DbContextOptionsBuilder<TestDbContext>>? ConfigureOptionsBuilder { get; set; }
   protected Action<ModelBuilder>? ConfigureModel { get; set; }

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper,
                                  IMigrationExecutionStrategy? migrationExecutionStrategy = null)
      : base(migrationExecutionStrategy)
   {
      DisableModelCache = true;

      var loggerFactory = CreateLoggerFactory(testOutputHelper);
      UseLoggerFactory(loggerFactory);
   }

   protected IDiagnosticsLogger<TCategory> CreateDiagnosticsLogger<TCategory>(ILoggingOptions? options = null, DiagnosticSource? diagnosticSource = null)
      where TCategory : LoggerCategory<TCategory>, new()
   {
      var loggerFactory = LoggerFactory ?? throw new InvalidOperationException($"The '{nameof(LoggerFactory)}' must be set first.");

      return new DiagnosticsLogger<TCategory>(loggerFactory, options ?? new LoggingOptions(),
                                              diagnosticSource ?? new DiagnosticListener(typeof(TCategory).ShortDisplayName()),
                                              new SqliteLoggingDefinitions(),
                                              new NullDbContextLogger());
   }

   protected override DbContextOptionsBuilder<TestDbContext> CreateOptionsBuilder(DbConnection? connection)
   {
      var builder = base.CreateOptionsBuilder(connection);
      ConfigureOptionsBuilder?.Invoke(builder);

      return builder;
   }

   /// <inheritdoc />
   protected override void ConfigureSqlite(SqliteDbContextOptionsBuilder builder)
   {
      base.ConfigureSqlite(builder);

      builder.AddBulkOperationSupport()
             .AddRowNumberSupport();
   }

   protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options)
   {
      var ctx = base.CreateContext(options);
      ctx.ConfigureModel = ConfigureModel;

      return ctx;
   }

   private ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper)
   {
      ArgumentNullException.ThrowIfNull(testOutputHelper);

      return _loggerFactoryCache.GetOrAdd(testOutputHelper, helper =>
                                                            {
                                                               var loggerConfig = new LoggerConfiguration()
                                                                                  .WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

                                                               return new LoggerFactory()
                                                                  .AddSerilog(loggerConfig.CreateLogger());
                                                            });
   }
}
