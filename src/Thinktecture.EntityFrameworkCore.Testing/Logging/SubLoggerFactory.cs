using Microsoft.Extensions.Logging;

namespace Thinktecture.Logging;

internal class SubLoggerFactory : ILoggerProvider
{
   private readonly Dictionary<string, SubLogger> _loggers = new(StringComparer.Ordinal);
   private readonly object _sync = new();

   private readonly ILoggerFactory _loggerFactory;

   public SubLoggerFactory(ILoggerFactory loggerFactory)
   {
      _loggerFactory = loggerFactory;
   }

   public ILogger CreateLogger(string categoryName)
   {
      lock (_sync)
      {
         if (!_loggers.TryGetValue(categoryName, out var logger))
         {
            logger = new SubLogger(_loggerFactory.CreateLogger(categoryName));
            _loggers[categoryName] = logger;
         }

         return logger;
      }
   }

   public void Dispose()
   {
      lock (_sync)
      {
         _loggers.Clear();
      }
   }
}
