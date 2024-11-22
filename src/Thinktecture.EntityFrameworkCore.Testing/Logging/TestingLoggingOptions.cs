using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture.EntityFrameworkCore.Testing;

namespace Thinktecture.Logging;

/// <summary>
/// Options provided to <see cref="ITestDbContextProvider{T}"/>
/// </summary>
public class TestingLoggingOptions : IDisposable
{
   /// <summary>
   /// Options without concrete logger.
   /// </summary>
   public static readonly TestingLoggingOptions Empty = Create(null, null, false, LogLevel.Information);

   /// <summary>
   /// Logger factory.
   /// </summary>
   public ILoggerFactory LoggerFactory { get; }

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
      TestingLogLevelSwitch logLevelSwitch,
      bool enableSensitiveDataLogging,
      LogLevel migrationLogLevel)
   {
      LoggerFactory = loggerFactory;
      LogLevelSwitch = logLevelSwitch;
      EnableSensitiveDataLogging = enableSensitiveDataLogging;
      MigrationLogLevel = migrationLogLevel;
   }

   /// <summary>
   /// Create a new instance of <see cref="TestingLoggingOptions"/>.
   /// </summary>
   /// <param name="loggerFactory">Logger factory.</param>
   /// <param name="serilogLogger">Serilog config.</param>
   /// <param name="enableSensitiveDataLogging">Indication whether EF should enable sensitive data logging or not.</param>
   /// <param name="migrationLogLevel">Log level to use during migrations.</param>
   /// <returns>A new instance of <see cref="TestingLoggingOptions"/>.</returns>
   public static TestingLoggingOptions Create(
      ILoggerFactory? loggerFactory,
      Serilog.ILogger? serilogLogger,
      bool enableSensitiveDataLogging,
      LogLevel migrationLogLevel)
   {
      var logLevelSwitch = new TestingLogLevelSwitch();

      var newLoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                                                                               {
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

      return new TestingLoggingOptions(newLoggerFactory, logLevelSwitch, enableSensitiveDataLogging, migrationLogLevel);
   }

   /// <inheritdoc />
   public void Dispose()
   {
      LoggerFactory.Dispose();
   }
}
