using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// A reference to SQLite temp table.
/// </summary>
public sealed partial class SqliteTempTableReference : ITempTableReference
{
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly DatabaseFacade _database;
   private readonly ITempTableNameLease _nameLease;
   private readonly bool _dropTableOnDispose;

   /// <inheritdoc />
   public string Name { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteTempTableReference"/>.
   /// </summary>
   /// <param name="logger">Logger</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="tableName">The name of the temp table.</param>
   /// <param name="database">Database facade.</param>
   /// <param name="nameLease">Leased table name that will be disposed along with the temp table.</param>
   /// <param name="dropTableOnDispose">Indication whether to drop the temp table on dispose or not.</param>
   public SqliteTempTableReference(IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                   ISqlGenerationHelper sqlGenerationHelper,
                                   string tableName,
                                   DatabaseFacade database,
                                   ITempTableNameLease nameLease,
                                   bool dropTableOnDispose)
   {
      Name = tableName ?? throw new ArgumentNullException(nameof(tableName));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _database = database ?? throw new ArgumentNullException(nameof(database));
      _nameLease = nameLease ?? throw new ArgumentNullException(nameof(nameLease));
      _dropTableOnDispose = dropTableOnDispose;
   }

   /// <inheritdoc />
   public void Dispose()
   {
      try
      {
         using var command = TryCreateCleanupCommand();

         if (command is null)
            return;

         command.ExecuteNonQuery();

         _database.CloseConnection();
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

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      try
      {
         await using var command = TryCreateCleanupCommand();

         if (command is null)
            return;

         await command.ExecuteNonQueryAsync();

         await _database.CloseConnectionAsync().ConfigureAwait(false);
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
      DbCommand? command = null;

      try
      {
         if (!_dropTableOnDispose)
            return null;

         var connection = _database.GetDbConnection();

         if (connection.State != ConnectionState.Open)
            return null;

         var sql = $"DROP TABLE IF EXISTS {_sqlGenerationHelper.DelimitIdentifier(Name, "temp")}";

         command = connection.CreateCommand();
         command.CommandText = sql;

         return command;
      }
      catch
      {
         command?.Dispose();
         throw;
      }
   }

   [LoggerMessage(Level = LogLevel.Warning,
                  Message = "Error during disposal of the temp table reference '{Name}'.")]
   private static partial void LogTempTableDisposalError(ILogger logger, Exception ex, string name);
}
