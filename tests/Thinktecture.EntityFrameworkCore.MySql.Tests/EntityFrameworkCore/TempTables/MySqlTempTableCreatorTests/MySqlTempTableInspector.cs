using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.TempTables.MySqlTempTableCreatorTests;

/// <summary>
/// Reads the structure and the content of MySQL temporary tables.
/// MySQL has no <c>INFORMATION_SCHEMA</c> entry for temporary tables, and a temporary table is visible to the session that created it only.
/// Therefore all methods run on the connection of the provided context. As long as the temp table is alive, this connection is open.
/// </summary>
internal static class MySqlTempTableInspector
{
   /// <summary>
   /// MySQL error "Table doesn't exist".
   /// </summary>
   public const int TABLE_DOES_NOT_EXIST = 1146;

   /// <summary>
   /// Gets the entity type used to create a temp table of <typeparamref name="T"/>: the entity type registered via <c>ConfigureTempTableEntity</c>
   /// or, if there is none, the regular entity type of <typeparamref name="T"/>.
   /// </summary>
   public static IEntityType GetTempTableEntityType<T>(DbContext context)
   {
      return context.Model.GetEntityType(EntityNameProvider.GetTempTableName(typeof(T)), typeof(T));
   }

   /// <summary>
   /// Opens the connection of the context until the returned object is disposed.
   /// A closed connection is reset by the connection pool, which drops temp tables on its own.
   /// Tests that observe the DROP of the library keep the connection open.
   /// </summary>
   public static async Task<IAsyncDisposable> KeepConnectionOpenAsync(DbContext context)
   {
      return await ConnectionScope.OpenAsync(context);
   }

   /// <summary>
   /// Gets the columns of the table in the order of the definition.
   /// </summary>
   public static async Task<IReadOnlyList<MySqlTempTableColumn>> GetColumnsAsync(DbContext context, string tableName)
   {
      await using var scope = await ConnectionScope.OpenAsync(context);
      await using var command = scope.CreateCommand($"SHOW FULL COLUMNS FROM {Quote(tableName)}");
      await using var reader = await command.ExecuteReaderAsync();
      var columns = new List<MySqlTempTableColumn>();

      while (await reader.ReadAsync())
      {
         columns.Add(new MySqlTempTableColumn(ReadString(reader, "Field") ?? throw new InvalidOperationException("The column has no name."),
                                              ReadString(reader, "Type") ?? throw new InvalidOperationException("The column has no type."),
                                              ReadString(reader, "Collation"),
                                              String.Equals(ReadString(reader, "Null"), "YES", StringComparison.Ordinal),
                                              ReadString(reader, "Key"),
                                              ReadString(reader, "Default"),
                                              ReadString(reader, "Extra")));
      }

      return columns;
   }

   /// <summary>
   /// Gets the names of the columns the primary key consists of in the order of the key.
   /// </summary>
   public static async Task<IReadOnlyList<string>> GetPrimaryKeyColumnsAsync(DbContext context, string tableName)
   {
      await using var scope = await ConnectionScope.OpenAsync(context);
      await using var command = scope.CreateCommand($"SHOW KEYS FROM {Quote(tableName)} WHERE Key_name = 'PRIMARY'");
      await using var reader = await command.ExecuteReaderAsync();
      var columns = new List<(int Position, string Name)>();

      while (await reader.ReadAsync())
      {
         columns.Add((Convert.ToInt32(reader["Seq_in_index"], CultureInfo.InvariantCulture), ReadString(reader, "Column_name") ?? throw new InvalidOperationException("The key column has no name.")));
      }

      return columns.OrderBy(column => column.Position).Select(column => column.Name).ToList();
   }

   /// <summary>
   /// Gets the <c>CREATE TABLE</c> statement of the server.
   /// </summary>
   public static async Task<string> ShowCreateTableAsync(DbContext context, string tableName)
   {
      await using var scope = await ConnectionScope.OpenAsync(context);
      await using var command = scope.CreateCommand($"SHOW CREATE TABLE {Quote(tableName)}");
      await using var reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();

      return ReadString(reader, "Create Table") ?? throw new InvalidOperationException("The server returned no statement.");
   }

   /// <summary>
   /// Gets the number of rows of the table.
   /// </summary>
   public static async Task<int> CountRowsAsync(DbContext context, string tableName)
   {
      await using var scope = await ConnectionScope.OpenAsync(context);
      await using var command = scope.CreateCommand($"SELECT COUNT(*) FROM {Quote(tableName)}");

      return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
   }

   /// <summary>
   /// Indicates whether the table exists for the session of the connection of the context.
   /// </summary>
   public static async Task<bool> ExistsAsync(DbContext context, string tableName)
   {
      await using var scope = await ConnectionScope.OpenAsync(context);
      await using var command = scope.CreateCommand($"SELECT 1 FROM {Quote(tableName)} LIMIT 1");

      try
      {
         await command.ExecuteScalarAsync();
         return true;
      }
      catch (MySqlException ex) when (ex.Number == TABLE_DOES_NOT_EXIST)
      {
         return false;
      }
   }

   private static string Quote(string identifier)
   {
      return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
   }

   // A column is returned as text or, depending on the server version, as binary text.
   private static string? ReadString(DbDataReader reader, string column)
   {
      return reader[column] switch
      {
         DBNull => null,
         byte[] bytes => Encoding.UTF8.GetString(bytes),
         var value => Convert.ToString(value, CultureInfo.InvariantCulture)
      };
   }

   // Keeps the connection open while a command runs. If it was open already (for example because of a temp table), it stays open.
   private sealed class ConnectionScope : IAsyncDisposable
   {
      private readonly DbContext _context;

      private ConnectionScope(DbContext context)
      {
         _context = context;
      }

      public static async Task<ConnectionScope> OpenAsync(DbContext context)
      {
         await context.Database.OpenConnectionAsync();
         return new ConnectionScope(context);
      }

      public DbCommand CreateCommand(string commandText)
      {
         var command = _context.Database.GetDbConnection().CreateCommand();
         command.CommandText = commandText;
         command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
         return command;
      }

      public async ValueTask DisposeAsync()
      {
         await _context.Database.CloseConnectionAsync();
      }
   }
}

/// <summary>
/// A column as reported by <c>SHOW FULL COLUMNS</c>.
/// </summary>
/// <param name="Name">Name of the column.</param>
/// <param name="Type">Data type including length, precision and unsigned flag, e.g. <c>varchar(50)</c>.</param>
/// <param name="Collation">Collation of text columns, otherwise <c>null</c>.</param>
/// <param name="IsNullable">Indication whether the column accepts <c>NULL</c>.</param>
/// <param name="Key">Key flag, e.g. <c>PRI</c>.</param>
/// <param name="Default">The default value, if there is one.</param>
/// <param name="Extra">Additional information like <c>auto_increment</c> or <c>DEFAULT_GENERATED</c>.</param>
internal sealed record MySqlTempTableColumn(
   string Name,
   string Type,
   string? Collation,
   bool IsNullable,
   string? Key,
   string? Default,
   string? Extra);
