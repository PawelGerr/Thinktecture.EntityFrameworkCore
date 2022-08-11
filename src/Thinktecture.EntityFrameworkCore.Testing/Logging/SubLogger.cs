using Microsoft.Extensions.Logging;

namespace Thinktecture.Logging;

internal class SubLogger : ILogger
{
   private readonly ILogger _logger;

   public SubLogger(ILogger logger)
   {
      _logger = logger;
   }

   public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
   {
      _logger.Log(logLevel, eventId, state, exception, formatter);
   }

   public bool IsEnabled(LogLevel logLevel)
   {
      return _logger.IsEnabled(logLevel);
   }

   public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull
   {
      return _logger.BeginScope(state);
   }
}
