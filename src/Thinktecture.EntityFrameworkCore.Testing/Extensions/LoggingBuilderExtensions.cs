using Microsoft.Extensions.Logging;
using Thinktecture.Logging;

namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="ILoggerFactory"/>.
/// </summary>
public static class LoggingBuilderExtensions
{
   /// <summary>
   /// Collects executed SQL statements and adds them to the <see cref="IReadOnlyCollection{T}"/>.
   /// </summary>
   /// <param name="loggingBuilder">Logging builder to register the statement collector with.</param>
   /// <returns></returns>
   /// <exception cref="ArgumentNullException"><paramref name="loggingBuilder"/> is <c>null</c>.</exception>
   public static IReadOnlyCollection<string> CollectExecutedCommands(
      this ILoggingBuilder loggingBuilder)
   {
      ArgumentNullException.ThrowIfNull(loggingBuilder);

      var sqlCommandLoggerProvider = new ExecutedCommandLoggerProvider();
      loggingBuilder.AddProvider(sqlCommandLoggerProvider);

      return sqlCommandLoggerProvider.Commands;
   }
}
