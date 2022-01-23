using Microsoft.Extensions.Logging;
using Serilog;
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
   /// Creates logging options.
   /// </summary>
   /// <returns>A new instance of <see cref="TestingLoggingOptions"/>.</returns>
   protected TestingLoggingOptions CreateLoggingOptions()
   {
      return TestingLoggingOptions.Create(_loggerFactory, _serilogLogger, _collectExecutedCommands, _enableSensitiveDataLogging, _migrationLogLevel);
   }
}
