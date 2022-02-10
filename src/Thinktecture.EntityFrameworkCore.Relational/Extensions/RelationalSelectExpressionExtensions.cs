using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture;

/// <summary>
/// For internal use only.
/// </summary>
public static class RelationalSelectExpressionExtensions
{
   private static readonly Lazy<Func<ITableBase, TableExpression>> _tableExpressionFactory = new(() =>
                                                                                                 {
                                                                                                    var tableBaseType = typeof(ITableBase);
                                                                                                    var ctorInfo = typeof(TableExpression).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { tableBaseType })
                                                                                                                   ?? throw new Exception($"Constructor for creation of {nameof(TableExpression)} not found.");

                                                                                                    var param = Expression.Parameter(tableBaseType);

                                                                                                    return Expression.Lambda<Func<ITableBase, TableExpression>>(Expression.New(ctorInfo, param), param).Compile();
                                                                                                 });

   private static readonly Lazy<Func<SelectExpression, List<(ColumnExpression Column, ValueComparer Comparer)>>> _getIdentifier = new(() =>
                                                                                                                                      {
                                                                                                                                         var selectType = typeof(SelectExpression);
                                                                                                                                         var fieldInfo = selectType.GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic)
                                                                                                                                                         ?? throw new Exception("The field 'SelectExpression._identifier' not found.");

                                                                                                                                         var param = Expression.Parameter(selectType);
                                                                                                                                         return Expression.Lambda<Func<SelectExpression, List<(ColumnExpression Column, ValueComparer Comparer)>>>(Expression.MakeMemberAccess(param, fieldInfo), param).Compile();
                                                                                                                                      });

   private static readonly Lazy<Func<SelectExpression, HashSet<string>>> _getUsedAliases = new(() =>
                                                                                               {
                                                                                                  var selectType = typeof(SelectExpression);
                                                                                                  var fieldInfo = selectType.GetField("_usedAliases", BindingFlags.Instance | BindingFlags.NonPublic)
                                                                                                                  ?? throw new Exception("The field 'SelectExpression._usedAliases' not found.");

                                                                                                  var param = Expression.Parameter(selectType);
                                                                                                  return Expression.Lambda<Func<SelectExpression, HashSet<string>>>(Expression.MakeMemberAccess(param, fieldInfo), param).Compile();
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
         selectExpression = selectExpression.CloneWithNewTable(tableWithMetadata);
      }

      var foundOldMetadata = tableWithMetadata.Metadata.TryGetValue(metadataName, out var oldMetadata);
      var newMetadata = addOrUpdateMetadata(oldMetadata);

      if (newMetadata is not null)
      {
         tableWithMetadata.Metadata[metadataName] = newMetadata;
      }
      else if (foundOldMetadata)
      {
         tableWithMetadata.Metadata.Remove(metadataName);
      }

      return selectExpression;
   }

   private static SelectExpression CloneWithNewTable(this SelectExpression selectExpression, TableWithMetadata tableWithMetadata)
   {
      var newTableExpression = _tableExpressionFactory.Value(tableWithMetadata);

      var newSelect = selectExpression.Update(selectExpression.Projection,
                                              new[] { newTableExpression },
                                              selectExpression.Predicate,
                                              selectExpression.GroupBy,
                                              selectExpression.Having,
                                              selectExpression.Orderings,
                                              selectExpression.Limit,
                                              selectExpression.Offset);
      _getIdentifier.Value(newSelect).AddRange(_getIdentifier.Value(selectExpression));
      var usedAliases = _getUsedAliases.Value(newSelect);

      foreach (var alias in _getUsedAliases.Value(selectExpression))
      {
         usedAliases.Add(alias);
      }

      return newSelect;
   }
}
