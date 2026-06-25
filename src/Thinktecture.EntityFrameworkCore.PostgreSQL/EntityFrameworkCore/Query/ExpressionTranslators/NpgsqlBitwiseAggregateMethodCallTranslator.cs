using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates <see cref="NpgsqlAggregateDbFunctionsExtensions.BitOr{T}"/>,
/// <see cref="NpgsqlAggregateDbFunctionsExtensions.BitAnd{T}"/> and
/// <see cref="NpgsqlAggregateDbFunctionsExtensions.BitXor{T}"/> into the native PostgreSQL
/// <c>bit_or</c>/<c>bit_and</c>/<c>bit_xor</c> aggregates.
/// </summary>
/// <remarks>
/// Returns <c>null</c> on every non-matching path so EF Core falls through to the next aggregate
/// translator. The aggregate reuses the group element selector's own type mapping (no cast), so
/// <see cref="short"/>/<see cref="int"/>/<see cref="long"/>, their nullable forms and
/// <c>[Flags]</c> enums round-trip unchanged.
/// </remarks>
internal sealed class NpgsqlBitwiseAggregateMethodCallTranslator : IAggregateMethodCallTranslator
{
   private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

   public NpgsqlBitwiseAggregateMethodCallTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory)
   {
      _sqlExpressionFactory = sqlExpressionFactory ?? throw new ArgumentNullException(nameof(sqlExpressionFactory));
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   public SqlExpression? Translate(
      MethodInfo method,
      EnumerableExpression source,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
   {
      if (method.DeclaringType != typeof(NpgsqlAggregateDbFunctionsExtensions))
         return null;

      var functionName = GetAggregateFunctionName(method.Name);

      if (functionName is null)
         return null;

      // EnumerableExpression.Selector is typed as Expression; only a SqlExpression carries a type mapping.
      if (source.Selector is not SqlExpression selector)
         return null;

      // bit_or(col) / bit_and(col) / bit_xor(col). Passing "source" carries DISTINCT/FILTER/ORDER BY modifiers.
      // No cast: bit_* returns the input type; reusing the selector's mapping round-trips enums and ints.
      return _sqlExpressionFactory.AggregateFunction(
         functionName,
         [selector],
         source,
         nullable: true,
         argumentsPropagateNullability: [false],
         returnType: method.ReturnType,
         typeMapping: selector.TypeMapping);
   }

   private static string? GetAggregateFunctionName(string methodName)
   {
      return methodName switch
      {
         nameof(NpgsqlAggregateDbFunctionsExtensions.BitOr) => "bit_or",
         nameof(NpgsqlAggregateDbFunctionsExtensions.BitAnd) => "bit_and",
         nameof(NpgsqlAggregateDbFunctionsExtensions.BitXor) => "bit_xor",
         _ => null
      };
   }
}
