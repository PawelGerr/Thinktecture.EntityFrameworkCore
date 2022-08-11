using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture;

/// <summary>
/// For internal use only.
/// </summary>
public static class RelationalSelectExpressionExtensions
{
   private static readonly Lazy<Action<TableExpression, TableWithMetadata>> _setTableWithMetadata = new(() =>
                                                                                                        {
                                                                                                           var tableFieldInfo = typeof(TableExpression).GetField("<Table>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                                                                                                                                ?? throw new Exception("The field 'SelectExpression.<Table>k__BackingField' not found.");

                                                                                                           return (tableExpression, tableWithMetadata) => tableFieldInfo.SetValue(tableExpression, tableWithMetadata);
                                                                                                        });

   /// <summary>
   /// For internal use only.
   /// </summary>
   public static SelectExpression AddTableMetadata(
      this SelectExpression selectExpression,
      string metadataName,
      Func<object?, object?> addOrUpdateMetadata)
   {
      var tables = selectExpression.Tables;

      if (tables.Count == 0)
         throw new InvalidOperationException($"No tables found to add metadata '{metadataName}' to.");

      if (tables.Count > 1)
         throw new InvalidOperationException($"Multiple tables found to add metadata '{metadataName}' to. Expressions: {String.Join(", ", tables.Select(t => t.Print()))}");

      var tableExpressionBase = selectExpression.Tables[0];

      if (tableExpressionBase is not TableExpression tableExpression)
         throw new NotSupportedException($"Metadata of type '{metadataName}' can be applied to tables only but found '{tableExpressionBase.GetType().Name}'. Expression: {tableExpressionBase.Print()}");

      if (tableExpression.Table is not TableWithMetadata tableWithMetadata)
      {
         tableWithMetadata = new TableWithMetadata(tableExpression.Table);

         _setTableWithMetadata.Value(tableExpression, tableWithMetadata);
      }

      tableWithMetadata.AddOrUpdateMetadata(metadataName, addOrUpdateMetadata);

      return selectExpression;
   }
}
