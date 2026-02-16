using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates extension methods on <see cref="NpgsqlDbFunctionsExtensions"/>.
/// </summary>
public sealed class NpgsqlDbFunctionsTranslator : IMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly IModel _model;

   internal NpgsqlDbFunctionsTranslator(
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

      if (method.DeclaringType != typeof(NpgsqlDbFunctionsExtensions))
         return null;

      switch (method.Name)
      {
         case nameof(NpgsqlDbFunctionsExtensions.Sum):
         {
            return CreateWindowFunctionExpression("SUM", arguments);
         }
         case nameof(NpgsqlDbFunctionsExtensions.Average):
         {
            return CreateWindowFunctionExpression("AVG", arguments);
         }
         case nameof(NpgsqlDbFunctionsExtensions.Max):
         {
            return CreateWindowFunctionExpression("MAX", arguments);
         }
         case nameof(NpgsqlDbFunctionsExtensions.Min):
         {
            return CreateWindowFunctionExpression("MIN", arguments);
         }
         case nameof(NpgsqlDbFunctionsExtensions.WindowFunction):
         {
            return CreateWindowFunction(arguments);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(NpgsqlDbFunctionsExtensions)}'.");
      }
   }

   private SqlExpression CreateWindowFunction(IReadOnlyList<SqlExpression> arguments)
   {
      var (functionName, returnType, useAsteriskWhenNoArguments) = (WindowFunction?)((SqlConstantExpression)arguments[1]).Value
                                                               ?? throw new ArgumentException("Window function must not be null");

      var orderByExpression = arguments[^1] as WindowFunctionOrderingsExpression;

      WindowFunctionPartitionByExpression? partitionByExpression;
      int numberOfArguments;

      if (orderByExpression is null)
      {
         partitionByExpression = arguments[^1] as WindowFunctionPartitionByExpression;
         numberOfArguments = partitionByExpression is null
                                ? arguments.Count - 2
                                : arguments.Count - 3;
      }
      else
      {
         partitionByExpression = arguments[^2] as WindowFunctionPartitionByExpression;
         numberOfArguments = partitionByExpression is null
                                ? arguments.Count - 3
                                : arguments.Count - 4;
      }

      var functionArgs = numberOfArguments <= 0
                            ? Array.Empty<SqlExpression>()
                            : arguments.Skip(2).Take(numberOfArguments).Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e)).ToArray();

      var partitionBy = partitionByExpression?.PartitionBy.Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e)).ToList();

      return new WindowFunctionExpression(functionName,
                                          useAsteriskWhenNoArguments,
                                          returnType,
                                          _typeMappingSource.FindMapping(returnType, _model),
                                          functionArgs,
                                          partitionBy,
                                          orderByExpression?.Orderings);
   }

   [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
   private SqlExpression CreateWindowFunctionExpression(string aggregateFunction, IReadOnlyList<SqlExpression> arguments)
   {
      var aggregate = arguments[1];
      var orderings = arguments[^1] as WindowFunctionOrderingsExpression;
      var partitionBy = arguments.Skip(2).Take(arguments.Count - (orderings is null ? 2 : 3)).Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e)).ToList();

      return new WindowFunctionExpression(aggregateFunction,
                                          false,
                                          aggregate.Type,
                                          aggregate.TypeMapping,
                                          new[] { aggregate },
                                          partitionBy,
                                          orderings?.Orderings);
   }
}
