using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Thinktecture.Logging;

// ReSharper disable once CheckNamespace
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
   [SuppressMessage("ReSharper", "CA2000")]
   public static IReadOnlyCollection<string> CollectExecutedCommands(
      this ILoggerFactory loggerFactory)
   {
      if (loggerFactory == null)
         throw new ArgumentNullException(nameof(loggerFactory));

      var sqlCommandLoggerProvider = new ExecutedCommandLoggerProvider();
      loggerFactory.AddProvider(sqlCommandLoggerProvider);

      return sqlCommandLoggerProvider.Commands;
   }
}
