using Microsoft.Extensions.Logging;
using Thinktecture.Logging;

namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="ILoggerFactory"/>.
/// </summary>
public static class LoggerFactoryExtensions
{
   /// <summary>
   /// Collects executed SQL statements and adds them to the <see cref="IReadOnlyCollection{T}"/>.
   /// </summary>
   /// <param name="loggerFactory">Logger factory to register the statement collector with.</param>
   /// <returns></returns>
   /// <exception cref="ArgumentNullException"></exception>
   public static IReadOnlyCollection<string> CollectExecutedCommands(
      this ILoggerFactory loggerFactory)
   {
      ArgumentNullException.ThrowIfNull(loggerFactory);

      var sqlCommandLoggerProvider = new ExecutedCommandLoggerProvider();
      loggerFactory.AddProvider(sqlCommandLoggerProvider);

      return sqlCommandLoggerProvider.Commands;
   }
}
