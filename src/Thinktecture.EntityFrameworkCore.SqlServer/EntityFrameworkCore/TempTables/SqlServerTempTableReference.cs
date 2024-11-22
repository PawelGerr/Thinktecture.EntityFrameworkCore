using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// A reference to SQL Server temp table.
/// </summary>
public sealed class SqlServerTempTableReference : ITempTableReference
{
   private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly DatabaseFacade _database;
   private readonly ITempTableNameLease _nameLease;
   private readonly bool _dropTableOnDispose;

   private bool _isDisposed;

   /// <inheritdoc />
   public string Name { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableReference"/>.
   /// </summary>
   /// <param name="logger">Logger</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="tableName">The name of the temp table.</param>
   /// <param name="database">Database facade.</param>
   /// <param name="nameLease">Leased table name that will be disposed along with the temp table.</param>
   /// <param name="dropTableOnDispose">Indication whether to drop the temp table on dispose or not.</param>
   public SqlServerTempTableReference(IDiagnosticsLogger<DbLoggerCategory.Query> logger,
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
      if (_isDisposed)
         return;

      _isDisposed = true;

      try
      {
         using var command = TryCreateCleanupCommand();

         if (command is null)
            return;

         command.ExecuteNonQuery();

         _database.CloseConnection();
      }
      catch (ObjectDisposedException ex)
      {
         _logger.Logger.LogWarning(ex, $"Trying to dispose of the temp table reference '{Name}' after the corresponding DbContext has been disposed.");
      }
      finally
      {
         _nameLease.Dispose();
      }
   }

   /// <inheritdoc />
   public async ValueTask DisposeAsync()
   {
      if (_isDisposed)
         return;

      _isDisposed = true;

      try
      {
         await using var command = TryCreateCleanupCommand();

         if (command is null)
            return;

         await command.ExecuteNonQueryAsync();

         await _database.CloseConnectionAsync().ConfigureAwait(false);
      }
      catch (ObjectDisposedException ex)
      {
         _logger.Logger.LogWarning(ex, $"Trying to dispose of the temp table reference '{Name}' after the corresponding DbContext has been disposed.");
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

         command = connection.CreateCommand();
         command.CommandText = $"""
                                IF(OBJECT_ID('tempdb..{Name}') IS NOT NULL)
                                    DROP TABLE {_sqlGenerationHelper.DelimitIdentifier(Name)};
                                """;
         return command;
      }
      catch
      {
         command?.Dispose();
         throw;
      }
   }
}
