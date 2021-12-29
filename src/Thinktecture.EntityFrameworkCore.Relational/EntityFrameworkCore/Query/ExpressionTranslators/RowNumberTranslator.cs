using System.Linq.Expressions;
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
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   internal RowNumberTranslator(IRelationalTypeMappingSource typeMappingSource)
   {
      _typeMappingSource = typeMappingSource;
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
         case nameof(RelationalDbFunctionsExtensions.OrderBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(AdjustConversion(e), true)).ToList();
            return new RowNumberClauseOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.OrderByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(AdjustConversion(e), false)).ToList();
            return new RowNumberClauseOrderingsExpression(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenBy):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(AdjustConversion(e), true));
            return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.ThenByDescending):
         {
            var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(AdjustConversion(e), false));
            return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
         }
         case nameof(RelationalDbFunctionsExtensions.RowNumber):
         {
            var partitionBy = arguments.Skip(1).Take(arguments.Count - 2).Select(AdjustConversion).ToList();
            var orderings = (RowNumberClauseOrderingsExpression)arguments[^1];
            return new RowNumberExpression(partitionBy, orderings.Orderings, RelationalTypeMapping.NullMapping);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(RelationalDbFunctionsExtensions)}'.");
      }
   }

   private SqlExpression AdjustConversion(SqlExpression sqlExpression)
   {
      if (sqlExpression is SqlUnaryExpression { OperatorType: ExpressionType.Convert, TypeMapping: null } sqlUnaryExpression)
      {
         var mapping = _typeMappingSource.FindMapping(sqlExpression.Type);

         if (mapping is not null)
            return new SqlUnaryExpression(sqlUnaryExpression.OperatorType, sqlUnaryExpression.Operand, sqlUnaryExpression.Type, mapping);
      }

      return sqlExpression;
   }
}
