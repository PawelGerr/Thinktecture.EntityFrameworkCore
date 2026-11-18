using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Holds a reference to a MySQL temporary table and manages its session-scoped lifetime.
/// </summary>
public sealed partial class MySqlTempTableReference : ITempTableReference
{
   private readonly DatabaseFacade _database;
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly ITempTableNameLease _nameLease;
   private readonly bool _dropTableOnDispose;
   private int _isDisposed;

   /// <inheritdoc />
   public string Name { get; }

   internal MySqlTempTableReference(
      DatabaseFacade database,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger,
      ISqlGenerationHelper sqlGenerationHelper,
      string name,
      ITempTableNameLease nameLease,
      bool dropTableOnDispose)
   {
      _database = database ?? throw new ArgumentNullException(nameof(database));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      Name = name ?? throw new ArgumentNullException(nameof(name));
      _nameLease = nameLease ?? throw new ArgumentNullException(nameof(nameLease));
      _dropTableOnDispose = dropTableOnDispose;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
         return;

      try
      {
         using var command = TryCreateCleanupCommand();
         if (command is null)
            return;

         try
         {
            command.ExecuteNonQuery();
         }
         finally
         {
            _database.CloseConnection();
         }
      }
      catch (Exception exception)
      {
         _logger.Logger.LogWarning(exception, "Error during disposal of MySQL temporary table '{Name}'.", Name);
      }
      finally
      {
         _nameLease.Dispose();
      }
   }

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
         return;

      try
      {
         await using var command = TryCreateCleanupCommand();
         if (command is null)
            return;

         try
         {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
         }
         finally
         {
            await _database.CloseConnectionAsync().ConfigureAwait(false);
         }
      }
      catch (Exception ex)
      {
         LogTempTableDisposalError(_logger.Logger, ex, Name);
      }
      finally
      {
         _nameLease.Dispose();
      }
   }

   private DbCommand? TryCreateCleanupCommand()
   {
      if (!_dropTableOnDispose)
         return null;

      var connection = _database.GetDbConnection();
      if (connection.State != ConnectionState.Open)
         return null;

      var command = connection.CreateCommand();
      command.CommandText = $"DROP TEMPORARY TABLE IF EXISTS {_sqlGenerationHelper.DelimitIdentifier(Name)};";
      command.Transaction = _database.CurrentTransaction?.GetDbTransaction();
      return command;
   }

   [LoggerMessage(Level = LogLevel.Warning,
                  Message = "Error during disposal of the temp table reference '{Name}'.")]
   private static partial void LogTempTableDisposalError(ILogger logger, Exception ex, string name);
}
