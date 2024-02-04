using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates extension methods like "RowNumber"
/// </summary>
public sealed class RelationalDbFunctionsTranslator : IMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;

   internal RelationalDbFunctionsTranslator(ISqlExpressionFactory sqlExpressionFactory)
   {
      _sqlExpressionFactory = sqlExpressionFactory;
   }

   /// <inheritdoc />
   public SqlExpression? Translate(
      SqlExpression? instance,
      MethodInfo method,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
   {
      ArgumentNullException.ThrowIfNull(method);
      ArgumentNullException.ThrowIfNull(arguments);

      if (method.DeclaringType != typeof(RelationalDbFunctionsExtensions))
         return null;

      switch (method.Name)
      {
         case nameof(RelationalDbFunctionsExtensions.PartitionBy):
         {
            return new WindowFunctionPartitionByExpression(arguments.Skip(1).ToList());
         }
         case nameof(RelationalDbFunctionsExtensions.OrderBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(_sqlExpressionFactory.ApplyDefaultTypeMapping(e), true)).ToList();
            return new WindowFunctionOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.OrderByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(_sqlExpressionFactory.ApplyDefaultTypeMapping(e), false)).ToList();
            return new WindowFunctionOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(_sqlExpressionFactory.ApplyDefaultTypeMapping(e), true));
            return ((WindowFunctionOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(_sqlExpressionFactory.ApplyDefaultTypeMapping(e), false));
            return ((WindowFunctionOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.RowNumber):
         {
            var partitionBy = arguments.Skip(1).Take(arguments.Count - 2).Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e)).ToList();
            var orderings = (WindowFunctionOrderingsExpression)arguments[^1];
            return new RowNumberExpression(partitionBy, orderings.Orderings, RelationalTypeMapping.NullMapping);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(RelationalDbFunctionsExtensions)}'.");
      }
   }
}
