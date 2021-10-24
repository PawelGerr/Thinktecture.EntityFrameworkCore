using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Thinktecture.Logging
{
   internal class ExecutedCommandLoggerProvider : ILoggerProvider, ILogger
   {
      private readonly ConcurrentQueue<string> _commands;

      public IReadOnlyCollection<string> Commands => _commands;

      public ExecutedCommandLoggerProvider()
      {
         _commands = new ConcurrentQueue<string>();
      }

      public ILogger CreateLogger(string categoryName)
      {
         if (categoryName == DbLoggerCategory.Database.Command.Name)
            return this;

         return NullLogger.Instance;
      }

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
      {
         if (eventId != RelationalEventId.CommandExecuted)
            return;

         var command = formatter(state, exception);
         _commands.Enqueue(command);
      }

      public bool IsEnabled(LogLevel logLevel)
      {
         return logLevel >= LogLevel.Information;
      }

      public void Dispose()
      {
      }

      public IDisposable BeginScope<TState>(TState state)
      {
         return new EmptyDisposable();
      }

      private struct EmptyDisposable : IDisposable
      {
         public void Dispose()
         {
         }
      }
   }
}
