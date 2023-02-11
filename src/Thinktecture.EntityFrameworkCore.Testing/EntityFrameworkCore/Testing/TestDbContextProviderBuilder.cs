using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore.Diagnostics;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.Logging;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Base class for builders of <see cref="ITestDbContextProvider{T}"/>.
/// </summary>
public abstract class TestDbContextProviderBuilder
{
   private Serilog.ILogger? _serilogLogger;
   private ILoggerFactory? _loggerFactory;
   private bool _enableSensitiveDataLogging;
   private bool _collectExecutedCommands;
   private LogLevel _migrationLogLevel = LogLevel.Information;
   private bool _disableModelCache;
   private IMigrationExecutionStrategy? _migrationExecutionStrategy;

   /// <summary>
   /// Specifies the migration strategy to use.
   /// Default is <see cref="IMigrationExecutionStrategy.Migrations"/>.
   /// </summary>
   /// <param name="migrationExecutionStrategy">Migration strategy to use.</param>
   /// <returns>Current builder for chaining</returns>
   public void UseMigrationExecutionStrategy(IMigrationExecutionStrategy migrationExecutionStrategy)
   {
      ArgumentNullException.ThrowIfNull(migrationExecutionStrategy);

      _migrationExecutionStrategy = migrationExecutionStrategy;
   }

   /// <summary>
   /// Indication whether collect executed commands or not.
   /// </summary>
   /// <returns>Current builder for chaining</returns>
   public void CollectExecutedCommands(bool collectExecutedCommands)
   {
      _collectExecutedCommands = collectExecutedCommands;
   }

   /// <summary>
   /// Sets the logger factory to be used by EF.
   /// </summary>
   /// <param name="loggerFactory">Logger factory to use.</param>
   /// <param name="enableSensitiveDataLogging">Enables or disables sensitive data logging.</param>
   protected void UseLogging(ILoggerFactory? loggerFactory, bool enableSensitiveDataLogging)
   {
      _loggerFactory = loggerFactory;
      _enableSensitiveDataLogging = enableSensitiveDataLogging;
   }

   /// <summary>
   /// Sets output helper to be used by Serilog which is passed to EF.
   /// </summary>
   /// <param name="testOutputHelper">XUnit output.</param>
   /// <param name="enableSensitiveDataLogging">Enables or disables sensitive data logging.</param>
   /// <param name="outputTemplate">The serilog output template.</param>
   protected void UseLogging(
      ITestOutputHelper? testOutputHelper,
      bool enableSensitiveDataLogging,
      string? outputTemplate)
   {
      _serilogLogger = testOutputHelper is null
                          ? null
                          : new LoggerConfiguration()
                            .WriteTo.TestOutput(testOutputHelper, outputTemplate: outputTemplate ?? "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                            .CreateLogger();

      UseLogging(null, enableSensitiveDataLogging);
   }

   /// <summary>
   /// Sets the log level during migrations.
   /// </summary>
   /// <param name="logLevel">Minimum log level to use during migrations.</param>
   protected void UseMigrationLogLevel(LogLevel logLevel)
   {
      _migrationLogLevel = logLevel;
   }

   /// <summary>
   /// Disables EF model cache.
   /// </summary>
   /// <returns>Current builder for chaining.</returns>
   protected void DisableModelCache(bool disableModelCache)
   {
      _disableModelCache = disableModelCache;
   }

   /// <summary>
   /// Disables "LoggingCacheTime" of EF which is required to be able to change the <see cref="LogLevel"/> at will.
   /// </summary>
   /// <param name="builder">Builder.</param>
   protected virtual void DisableLoggingCacheTime(DbContextOptionsBuilder builder)
   {
      builder.AddOrUpdateExtension<CoreOptionsExtension>(extension => extension.WithLoggingCacheTime(TimeSpan.Zero));
   }

   /// <summary>
   /// Applies default settings to provided <paramref name="dbContextOptionsBuilder"/>.
   /// </summary>
   /// <param name="state">Current building state.</param>
   /// <param name="dbContextOptionsBuilder">Builder to apply settings to.</param>
   protected virtual void ApplyDefaultConfiguration(
      TestDbContextProviderBuilderState state,
      DbContextOptionsBuilder dbContextOptionsBuilder)
   {
      state.MigrationExecutionStrategy ??= _migrationExecutionStrategy;

      dbContextOptionsBuilder.UseLoggerFactory(state.LoggingOptions.LoggerFactory)
                             .EnableSensitiveDataLogging(state.LoggingOptions.EnableSensitiveDataLogging);

      DisableLoggingCacheTime(dbContextOptionsBuilder);

      if (_collectExecutedCommands)
      {
         state.CommandCapturingInterceptor ??= new CommandCapturingInterceptor();
         dbContextOptionsBuilder.AddInterceptors(state.CommandCapturingInterceptor);
      }

      if (_disableModelCache)
         dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, CachePerContextModelCacheKeyFactory>();
   }

   /// <summary>
   /// Creates logging options.
   /// </summary>
   /// <returns>A new instance of <see cref="TestingLoggingOptions"/>.</returns>
   protected TestingLoggingOptions CreateLoggingOptions()
   {
      return CreateLoggingOptions(_loggerFactory, _serilogLogger);
   }

   /// <summary>
   /// Creates logging options.
   /// </summary>
   /// <returns>A new instance of <see cref="TestingLoggingOptions"/>.</returns>
   public TestingLoggingOptions CreateLoggingOptions(ILoggerFactory? loggerFactory, Serilog.ILogger? serilogLogger)
   {
      return TestingLoggingOptions.Create(loggerFactory, serilogLogger, _enableSensitiveDataLogging, _migrationLogLevel);
   }
}
