using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;
using MySqlFunctions = Thinktecture.MySqlDbFunctionsExtensions;
using MySqlAggregateWindowFunctions = Thinktecture.MySqlWindowAggregateDbFunctionsExtensions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates extension methods on <see cref="MySqlDbFunctionsExtensions" />.
/// </summary>
public sealed class MySqlDbFunctionsTranslator : IMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly IModel _model;

   internal MySqlDbFunctionsTranslator(
      ISqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource,
      IModel model)
   {
      _sqlExpressionFactory = sqlExpressionFactory;
      _typeMappingSource = typeMappingSource;
      _model = model;
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

      if (method.DeclaringType == typeof(MySqlAggregateWindowFunctions))
         return CreateAggregateWindowFunction(method, arguments);

      if (method.DeclaringType != typeof(MySqlFunctions))
         return null;

      return method.Name switch
      {
         nameof(MySqlFunctions.WindowFunction) => CreateWindowFunction(arguments),
         nameof(MySqlFunctions.NTile) => CreateNTile(arguments),
         _ => throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(MySqlFunctions)}'.")
      };
   }

   private SqlExpression CreateAggregateWindowFunction(MethodInfo method, IReadOnlyList<SqlExpression> arguments)
   {
      var functionName = method.Name switch
      {
         nameof(MySqlAggregateWindowFunctions.Sum) => "SUM",
         nameof(MySqlAggregateWindowFunctions.Average) => "AVG",
         nameof(MySqlAggregateWindowFunctions.Max) => "MAX",
         nameof(MySqlAggregateWindowFunctions.Min) => "MIN",
         _ => throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(MySqlAggregateWindowFunctions)}'.")
      };

      var hasOrderBy = arguments[^1] is WindowFunctionOrderingsExpression;
      var partitionEnd = hasOrderBy ? arguments.Count - 1 : arguments.Count;
      var argument = _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]);
      var partitions = arguments.Skip(2).Take(partitionEnd - 2)
                            .Select(expression => _sqlExpressionFactory.ApplyDefaultTypeMapping(expression))
                            .ToArray();
      var orderings = hasOrderBy ? ((WindowFunctionOrderingsExpression)arguments[^1]).Orderings : null;

      // MAX/MIN return the operand's own type; its mapping carries value converters (e.g. enum to string).
      var typeMapping = functionName is "MAX" or "MIN" && argument.TypeMapping is not null
                           ? argument.TypeMapping
                           : _typeMappingSource.FindMapping(method.ReturnType, _model);

      return new WindowFunctionExpression(functionName,
                                          false,
                                          method.ReturnType,
                                          typeMapping,
                                          [argument],
                                          partitions,
                                          orderings);
   }

   private SqlExpression CreateWindowFunction(IReadOnlyList<SqlExpression> arguments)
   {
      var function = (WindowFunction?)((SqlConstantExpression)arguments[1]).Value
                     ?? throw new ArgumentException("Window function must not be null.");
      var orderings = arguments[^1] as WindowFunctionOrderingsExpression;
      var partitionBy = arguments[^1] as WindowFunctionPartitionByExpression;
      var numberOfArguments = arguments.Count - 2;

      if (orderings is not null)
      {
         partitionBy = arguments.Count > 3 ? arguments[^2] as WindowFunctionPartitionByExpression : null;
         numberOfArguments = arguments.Count - (partitionBy is null ? 3 : 4);
      }
      else if (partitionBy is not null)
      {
         numberOfArguments = arguments.Count - 3;
      }

      var functionArguments = numberOfArguments > 0
                                ? arguments.Skip(2).Take(numberOfArguments).Select(argument => _sqlExpressionFactory.ApplyDefaultTypeMapping(argument)).ToArray()
                                : Array.Empty<SqlExpression>();

      return new WindowFunctionExpression(function.Name,
                                          function.UseAsteriskWhenNoArguments,
                                          function.ReturnType,
                                          _typeMappingSource.FindMapping(function.ReturnType, _model),
                                          functionArguments,
                                          partitionBy?.PartitionBy.Select(argument => _sqlExpressionFactory.ApplyDefaultTypeMapping(argument)).ToArray(),
                                          orderings?.Orderings);
   }

   private SqlExpression CreateNTile(IReadOnlyList<SqlExpression> arguments)
   {
      var bucketCount = _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]);
      var orderings = arguments[^1] as WindowFunctionOrderingsExpression;
      var partitionCount = arguments.Count - (orderings is null ? 2 : 3);
      var partitionBy = arguments.Skip(2).Take(partitionCount).Select(argument => _sqlExpressionFactory.ApplyDefaultTypeMapping(argument)).ToArray();

      return new WindowFunctionExpression("NTILE",
                                          false,
                                          typeof(int),
                                          _typeMappingSource.FindMapping(typeof(int), _model),
                                          new[] { bucketCount },
                                          partitionBy,
                                          orderings?.Orderings);
   }
}