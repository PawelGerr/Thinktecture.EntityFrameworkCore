using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.Testing;

namespace Thinktecture.Logging;

/// <summary>
/// Options provided to <see cref="ITestDbContextProvider{T}"/>
/// </summary>
public class TestingLoggingOptions : IDisposable
{
   /// <summary>
   /// Logger factory.
   /// </summary>
   public ILoggerFactory LoggerFactory { get; }

   /// <summary>
   /// Executed commands, if the feature was activated.
   /// </summary>
   public IReadOnlyCollection<string>? ExecutedCommands { get; }

   /// <summary>
   /// Log level switch.
   /// </summary>
   public TestingLogLevelSwitch LogLevelSwitch { get; }

   /// <summary>
   /// Indication whether EF should enable sensitive data logging or not.
   /// </summary>
   public bool EnableSensitiveDataLogging { get; }

   /// <summary>
   /// Log level to use during migrations.
   /// </summary>
   public LogLevel MigrationLogLevel { get; }

   private TestingLoggingOptions(
      ILoggerFactory loggerFactory,
      IReadOnlyCollection<string>? executedCommands,
      TestingLogLevelSwitch logLevelSwitch,
      bool enableSensitiveDataLogging,
      LogLevel migrationLogLevel)
   {
      LoggerFactory = loggerFactory;
      ExecutedCommands = executedCommands;
      LogLevelSwitch = logLevelSwitch;
      EnableSensitiveDataLogging = enableSensitiveDataLogging;
      MigrationLogLevel = migrationLogLevel;
   }

   /// <summary>
   /// Create a new instance of <see cref="TestingLoggingOptions"/>.
   /// </summary>
   /// <param name="loggerFactory">Logger factory.</param>
   /// <param name="serilogLogger">Serilog config.</param>
   /// <param name="collectExecutedCommands">Indication whether to collect executed commands or not.</param>
   /// <param name="enableSensitiveDataLogging">Indication whether EF should enable sensitive data logging or not.</param>
   /// <param name="migrationLogLevel">Log level to use during migrations.</param>
   /// <returns>A new instance of <see cref="TestingLoggingOptions"/>.</returns>
   public static TestingLoggingOptions Create(
      ILoggerFactory? loggerFactory,
      Serilog.ILogger? serilogLogger,
      bool collectExecutedCommands,
      bool enableSensitiveDataLogging,
      LogLevel migrationLogLevel)
   {
      IReadOnlyCollection<string>? executedCommands = null;
      var logLevelSwitch = new TestingLogLevelSwitch();

      var newLoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                                                                               {
                                                                                  if (collectExecutedCommands)
                                                                                     executedCommands = builder.CollectExecutedCommands();

                                                                                  if (loggerFactory is not null)
                                                                                     builder.AddProvider(new SubLoggerFactory(loggerFactory));

                                                                                  if (serilogLogger is not null)
                                                                                     builder.AddSerilog(serilogLogger);

                                                                                  builder.Services.Configure<LoggerFilterOptions>(options =>
                                                                                                                                  {
                                                                                                                                     options.MinLevel = LogLevel.Trace;
                                                                                                                                     options.Rules.Clear();
                                                                                                                                  });
                                                                                  builder.AddFilter(level => level >= logLevelSwitch.MinimumLogLevel);
                                                                               });

      return new TestingLoggingOptions(newLoggerFactory, executedCommands, logLevelSwitch, enableSensitiveDataLogging, migrationLogLevel);
   }

   /// <inheritdoc />
   public void Dispose()
   {
      LoggerFactory.Dispose();
   }
}
