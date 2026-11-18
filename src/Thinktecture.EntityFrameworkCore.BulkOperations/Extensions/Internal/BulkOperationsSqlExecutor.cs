using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace Thinktecture.Internal;

/// <summary>
/// This is an internal API. It may be changed or removed without notice.
/// </summary>
// No extension methods: IDEs offer extension methods of namespaces that are not imported, so they would appear next to ExecuteSqlRawAsync on DatabaseFacade.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BulkOperationsSqlExecutor
{
   /// <summary>
   /// Executes a statement without parameters. Same as <c>ExecuteSqlRawAsync</c>, but the text is sent unchanged.
   /// <c>ExecuteSqlRawAsync</c> applies <c>String.Format</c> to the text, also without parameters, so a brace in a default value or in an identifier throws <see cref="FormatException"/>.
   /// </summary>
   /// <param name="database">The database facade of the context.</param>
   /// <param name="sql">The statement.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="database"/> or <paramref name="sql"/> is <c>null</c>.</exception>
   public static Task<int> ExecuteNonQueryAsync(DatabaseFacade database, string sql, CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(database);
      ArgumentNullException.ThrowIfNull(sql);

      return ExecuteNonQueryAsync(database, database.GetService<IRawSqlCommandBuilder>().Build(sql), ReadOnlyDictionary<string, object?>.Empty, cancellationToken);
   }

   /// <summary>
   /// Executes a command. Same as <c>ExecuteSqlRawAsync</c>, but with a command and the parameter values of EF Core.
   /// </summary>
   /// <param name="database">The database facade of the context.</param>
   /// <param name="command">The command to execute.</param>
   /// <param name="parameterValues">The parameter values of the command.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The number of affected rows.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="database"/>, <paramref name="command"/> or <paramref name="parameterValues"/> is <c>null</c>.</exception>
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   public static async Task<int> ExecuteNonQueryAsync(
      DatabaseFacade database,
      IRelationalCommand command,
      IReadOnlyDictionary<string, object?> parameterValues,
      CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(database);
      ArgumentNullException.ThrowIfNull(command);
      ArgumentNullException.ThrowIfNull(parameterValues);

      var accessor = (IDatabaseFacadeDependenciesAccessor)database;
      var dependencies = (IRelationalDatabaseFacadeDependencies)accessor.Dependencies;
      var criticalSection = dependencies.CoreOptions.AreThreadSafetyChecksEnabled ? dependencies.ConcurrencyDetector.EnterCriticalSection() : (ConcurrencyDetectorCriticalSectionDisposer?)null;

      try
      {
         var parameterObject = new RelationalCommandParameterObject(dependencies.RelationalConnection, parameterValues, null, accessor.Context, dependencies.CommandLogger, CommandSource.ExecuteSqlRaw);

         return await command.ExecuteNonQueryAsync(parameterObject, cancellationToken).ConfigureAwait(false);
      }
      finally
      {
         criticalSection?.Dispose();
      }
   }
}
