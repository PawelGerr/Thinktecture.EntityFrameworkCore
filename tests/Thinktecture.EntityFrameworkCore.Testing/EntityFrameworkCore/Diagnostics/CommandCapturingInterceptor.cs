using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Interceptor for capturing commands.
/// </summary>
public class CommandCapturingInterceptor : DbCommandInterceptor
{
   private readonly ConcurrentQueue<string> _commands;

   /// <summary>
   /// Captured commands.
   /// </summary>
   public IReadOnlyCollection<string> Commands => _commands;

   /// <summary>
   /// Initializes new instance of <see cref="CommandCapturingInterceptor"/>.
   /// </summary>
   public CommandCapturingInterceptor()
   {
      _commands = new ConcurrentQueue<string>();
   }

   /// <inheritdoc />
   public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
   {
      CaptureCommandText(command);

      return base.ReaderExecuted(command, eventData, result);
   }

   /// <inheritdoc />
   public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = new CancellationToken())
   {
      CaptureCommandText(command);

      return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
   }

   /// <inheritdoc />
   public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
   {
      CaptureCommandText(command);

      return base.ScalarExecuted(command, eventData, result);
   }

   /// <inheritdoc />
   public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = new CancellationToken())
   {
      CaptureCommandText(command);

      return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
   }

   /// <inheritdoc />
   public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
   {
      CaptureCommandText(command);

      return base.NonQueryExecuted(command, eventData, result);
   }

   /// <inheritdoc />
   public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = new CancellationToken())
   {
      CaptureCommandText(command);

      return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
   }

   private void CaptureCommandText(DbCommand command)
   {
      _commands.Enqueue(command.CommandText);
   }
}
