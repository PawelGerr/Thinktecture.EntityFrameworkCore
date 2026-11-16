using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microting.EntityFrameworkCore.MySql.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Internal;

/// <summary>
/// Builds the <c>CHARACTER SET</c> and <c>COLLATE</c> clauses of a column or a table.
/// The provider puts the charset and the collation that its migrations write on the tables and columns of the design-time relational model,
/// including the levels property, complex type member, entity type and model, and the collation of char-based Guid columns.
/// </summary>
internal static class MySqlColumnCharSet
{
   /// <summary>
   /// Gets the clauses of a column, for example <c> CHARACTER SET utf8mb4 COLLATE utf8mb4_bin</c>, or an empty string.
   /// The result is empty when the column type has no charset (the store type decides, not the CLR type: a value-converted enum is a varchar,
   /// a string mapped to json has no charset) or when the column type already contains a charset or a collation.
   /// </summary>
   public static string GetColumnClauses(IModel designTimeModel, StoreObjectIdentifier table, string columnName, string columnType)
   {
      if (!SupportsCharSet(columnType) || HasCharSetOrCollation(columnType))
         return String.Empty;

      var column = FindTable(designTimeModel, table)?.FindColumn(columnName);

      return column is null ? String.Empty : GetClauses(column, column.Collation);
   }

   /// <summary>
   /// Gets the clauses of a table, for example <c> CHARACTER SET utf8mb4</c>, or an empty string.
   /// </summary>
   public static string GetTableClauses(IModel designTimeModel, StoreObjectIdentifier table)
   {
      var relationalTable = FindTable(designTimeModel, table);

      return relationalTable is null ? String.Empty : GetClauses(relationalTable, null);
   }

   // MySQL accepts CHARACTER SET and COLLATE for character and enumeration columns only.
   public static bool SupportsCharSet(string columnType)
   {
      return MySqlTempTableCreator.GetBaseType(columnType) is "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" or "enum" or "set";
   }

   // shortcut: an enum or set value that contains one of these words also counts, then the clauses of the model are omitted; parse the type when this matters.
   private static bool HasCharSetOrCollation(string columnType)
   {
      return columnType.Contains("CHARACTER SET", StringComparison.OrdinalIgnoreCase)
             || columnType.Contains("CHARSET", StringComparison.OrdinalIgnoreCase)
             || columnType.Contains("COLLATE", StringComparison.OrdinalIgnoreCase);
   }

   private static ITable? FindTable(IModel designTimeModel, StoreObjectIdentifier table)
   {
      return designTimeModel.GetRelationalModel().FindTable(table.Name, table.Schema);
   }

   // The provider defines the name of the charset annotation in an internal namespace only.
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private static string GetClauses(IReadOnlyAnnotatable annotatable, string? collation)
   {
      var charSet = annotatable[MySqlAnnotationNames.CharSet] as string;
      collation ??= annotatable[RelationalAnnotationNames.Collation] as string;

      var clauses = new StringBuilder();

      if (!String.IsNullOrEmpty(charSet))
         clauses.Append(" CHARACTER SET ").Append(charSet);

      if (!String.IsNullOrEmpty(collation))
         clauses.Append(" COLLATE ").Append(collation);

      return clauses.ToString();
   }
}
