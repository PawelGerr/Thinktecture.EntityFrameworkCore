using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal sealed class MySqlBitwiseAggregateMethodCallTranslator : IAggregateMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;

   public MySqlBitwiseAggregateMethodCallTranslator(ISqlExpressionFactory sqlExpressionFactory)
   {
      _sqlExpressionFactory = sqlExpressionFactory ?? throw new ArgumentNullException(nameof(sqlExpressionFactory));
   }

   /// <inheritdoc />
   public SqlExpression? Translate(
      MethodInfo method,
      EnumerableExpression source,
      IReadOnlyList<SqlExpression> arguments,
      IDiagnosticsLogger<DbLoggerCategory.Query> logger)
   {
      if (method.DeclaringType != typeof(MySqlAggregateDbFunctionsExtensions))
         return null;

      var functionName = method.Name switch
      {
         nameof(MySqlAggregateDbFunctionsExtensions.BitOr) => "BIT_OR",
         nameof(MySqlAggregateDbFunctionsExtensions.BitAnd) => "BIT_AND",
         nameof(MySqlAggregateDbFunctionsExtensions.BitXor) => "BIT_XOR",
         _ => null
      };

      if (functionName is null || source.Selector is not SqlExpression selector)
         return null;

      if (source.Predicate is not null)
      {
         var nullValue = _sqlExpressionFactory.Constant(null, selector.Type, selector.TypeMapping);
         selector = _sqlExpressionFactory.Case([new CaseWhenClause(source.Predicate, selector)], nullValue);
      }

      // MySQL has no DISTINCT for BIT_*; it is a no-op for OR/AND but changes XOR.
      if (source.IsDistinct && functionName == "BIT_XOR")
         throw new NotSupportedException("MySQL does not support DISTINCT with BIT_XOR.");

      var typeMapping = ((SqlExpression)source.Selector).TypeMapping;
      var returnType = method.ReturnType;
      var nullableReturnType = returnType.IsValueType && Nullable.GetUnderlyingType(returnType) is null
                                  ? typeof(Nullable<>).MakeGenericType(returnType)
                                  : returnType;

      var bitAggregate = _sqlExpressionFactory.Function(functionName, [selector], nullable: true, argumentsPropagateNullability: [false], returnType: typeof(long));
      var signed = _sqlExpressionFactory.Convert(bitAggregate, nullableReturnType, typeMapping);

      // BIT_* returns BIGINT UNSIGNED (all ones on an empty group); an empty group yields NULL instead.
      var count = _sqlExpressionFactory.Function("COUNT", [selector], nullable: false, argumentsPropagateNullability: [false], returnType: typeof(long));
      var isEmpty = _sqlExpressionFactory.Equal(count, _sqlExpressionFactory.Constant(0L));
      var emptyResult = _sqlExpressionFactory.Constant(null, nullableReturnType, typeMapping);

      return _sqlExpressionFactory.Case([new CaseWhenClause(isEmpty, emptyResult)], signed);
   }
}
