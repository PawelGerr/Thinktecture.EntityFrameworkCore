using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates extension methods like "RowNumber"
/// </summary>
public sealed class SqlServerDbFunctionsTranslator : IMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;

   internal SqlServerDbFunctionsTranslator(ISqlExpressionFactory sqlExpressionFactory)
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

      if (method.DeclaringType != typeof(SqlServerDbFunctionsExtensions))
         return null;

      switch (method.Name)
      {
         case nameof(SqlServerDbFunctionsExtensions.Sum):
         {
            return CreateWindowFunctionExpression("SUM", arguments);
         }
         case nameof(SqlServerDbFunctionsExtensions.Average):
         {
            return CreateWindowFunctionExpression("AVG", arguments);
         }
         case nameof(SqlServerDbFunctionsExtensions.Max):
         {
            return CreateWindowFunctionExpression("MAX", arguments);
         }
         case nameof(SqlServerDbFunctionsExtensions.Min):
         {
            return CreateWindowFunctionExpression("MIN", arguments);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(SqlServerDbFunctionsExtensions)}'.");
      }
   }

   [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
   private SqlExpression CreateWindowFunctionExpression(string aggregateFunction, IReadOnlyList<SqlExpression> arguments)
   {
      var aggregate = arguments[1];
      var aggregateExpression = new SqlServerAggregateFunctionExpression(aggregateFunction,
                                                                         new[] { aggregate },
                                                                         Array.Empty<OrderingExpression>(),
                                                                         true,
                                                                         new[] { false },
                                                                         aggregate.Type,
                                                                         aggregate.TypeMapping);
      var orderings = arguments[^1] as WindowFunctionOrderingsExpression;
      var partitionBy = arguments.Skip(2).Take(arguments.Count - (orderings is null ? 2 : 3)).Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e)).ToList();
      return new WindowFunctionExpression(aggregateExpression, partitionBy, orderings?.Orderings);
   }
}
