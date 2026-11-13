using Microsoft.Extensions.Logging;

namespace Thinktecture.Logging;

/// <summary>
/// Log level switch for controlling the log level during tests.
/// </summary>
public class TestingLogLevelSwitch
{
   /// <summary>
   /// Minimum log level.
   /// </summary>
   public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
}
