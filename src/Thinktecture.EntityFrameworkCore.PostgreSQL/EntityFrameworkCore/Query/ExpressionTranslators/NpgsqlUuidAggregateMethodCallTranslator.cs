using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Rewrites <c>max</c>/<c>min</c> aggregates over <c>uuid</c> columns into
/// <c>max(col::text)::uuid</c> / <c>min(col::text)::uuid</c> because PostgreSQL has no native
/// <c>max(uuid)</c>/<c>min(uuid)</c> aggregate.
/// </summary>
/// <remarks>
/// Returns <c>null</c> on every non-matching path so EF Core falls through to the built-in
/// Npgsql aggregate translator unchanged. Aggregate plugins run before built-in translators, so
/// for <c>uuid</c> operands this translator takes over.
/// </remarks>
internal sealed class NpgsqlUuidAggregateMethodCallTranslator : IAggregateMethodCallTranslator
{
   private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   public NpgsqlUuidAggregateMethodCallTranslator(
      NpgsqlSqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource)
   {
      _sqlExpressionFactory = sqlExpressionFactory ?? throw new ArgumentNullException(nameof(sqlExpressionFactory));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   public SqlExpression? Translate(
      MethodInfo method,
      EnumerableExpression source,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
   {
      if (method.DeclaringType != typeof(Queryable))
         return null;

      var methodInfo = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

      var functionName = GetAggregateFunctionName(methodInfo);

      if (functionName is null)
         return null;

      // EnumerableExpression.Selector is typed as Expression; only a SqlExpression can carry a type mapping.
      if (source.Selector is not SqlExpression selector)
         return null;

      // Trigger exclusively on the "uuid" store type. This covers Guid and Guid?, while a Guid
      // mapped to e.g. "text" via a value converter is correctly left to the built-in translator.
      if (selector.TypeMapping?.StoreType != "uuid")
         return null;

      var textMapping = _typeMappingSource.FindMapping(typeof(string));

      // max(col::text) / min(col::text). Passing "source" carries DISTINCT/FILTER/ORDER BY modifiers.
      var castToText = _sqlExpressionFactory.Convert(selector, typeof(string), textMapping);
      var aggregate = _sqlExpressionFactory.AggregateFunction(
         functionName,
         [castToText],
         source,
         nullable: true,
         argumentsPropagateNullability: [false],
         returnType: typeof(string),
         typeMapping: textMapping);

      // (max(col::text))::uuid — reuse the original uuid mapping and CLR type (Guid or Guid?).
      return _sqlExpressionFactory.Convert(aggregate, selector.Type, selector.TypeMapping);
   }

   private static string? GetAggregateFunctionName(MethodInfo methodInfo)
   {
      if (methodInfo == QueryableMethods.MaxWithoutSelector || methodInfo == QueryableMethods.MaxWithSelector)
         return "max";

      if (methodInfo == QueryableMethods.MinWithoutSelector || methodInfo == QueryableMethods.MinWithSelector)
         return "min";

      return null;
   }
}
