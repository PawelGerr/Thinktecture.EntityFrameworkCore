using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translated extension method "RowNumber"
/// </summary>
public sealed class RowNumberTranslator : IMethodCallTranslator
{
   /// <inheritdoc />
   public SqlExpression? Translate(
      SqlExpression? instance,
      MethodInfo method,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
   {
      if (method == null)
         throw new ArgumentNullException(nameof(method));
      if (arguments == null)
         throw new ArgumentNullException(nameof(arguments));

      if (method.DeclaringType != typeof(RelationalDbFunctionsExtensions))
         return null;

      switch (method.Name)
      {
         case nameof(RelationalDbFunctionsExtensions.OrderBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, true)).ToList();
            return new RowNumberClauseOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.OrderByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, false)).ToList();
            return new RowNumberClauseOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, true));
            return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, false));
            return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.RowNumber):
         {
            var partitionBy = arguments.Skip(1).Take(arguments.Count - 2).ToList();
            var orderings = (RowNumberClauseOrderingsExpression)arguments[^1];
            return new RowNumberExpression(partitionBy, orderings.Orderings, RelationalTypeMapping.NullMapping);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(RelationalDbFunctionsExtensions)}'.");
      }
   }
}
