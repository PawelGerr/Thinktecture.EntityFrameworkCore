using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;

namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="ITestOutputHelper"/>.
/// </summary>
public static class TestOutputHelperExtensions
{
   /// <summary>
   /// Creates a <see cref="ILoggerFactory"/> out of <paramref name="testOutputHelper"/>.
   /// </summary>
   /// <param name="testOutputHelper">Output to write logs to.</param>
   /// <param name="outputTemplate">Serilog output template.</param>
   /// <returns>A new instance of <see cref="ILoggerFactory"/>.</returns>
   public static ILoggerFactory ToLoggerFactory(
      this ITestOutputHelper testOutputHelper,
      string? outputTemplate = null)
   {
      var loggerConfig = new LoggerConfiguration()
                         .WriteTo.TestOutput(testOutputHelper, outputTemplate: outputTemplate ?? "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

      return LoggerFactory.Create(builder => builder.AddSerilog(loggerConfig.CreateLogger(), true));
   }
}
