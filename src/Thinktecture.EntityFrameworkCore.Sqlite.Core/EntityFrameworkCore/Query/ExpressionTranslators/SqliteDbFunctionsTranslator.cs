using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Translates extension methods on <see cref="SqliteDbFunctionsExtensions"/>.
/// </summary>
public sealed class SqliteDbFunctionsTranslator : IMethodCallTranslator
{
   private readonly ISqlExpressionFactory _sqlExpressionFactory;
   private readonly IRelationalTypeMappingSource _typeMappingSource;
   private readonly IModel _model;

   internal SqliteDbFunctionsTranslator(
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

      if (method.DeclaringType != typeof(SqliteDbFunctionsExtensions))
         return null;

      switch (method.Name)
      {
         case nameof(SqliteDbFunctionsExtensions.NTile):
         {
            return CreateNTile(arguments);
         }
         default:
            throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(SqliteDbFunctionsExtensions)}'.");
      }
   }

   private SqlExpression CreateNTile(IReadOnlyList<SqlExpression> arguments)
   {
      var bucketCount = _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]);
      var orderings = arguments[^1] as WindowFunctionOrderingsExpression;
      var partitionEnd = arguments.Count - (orderings is null ? 0 : 1);
      var partitionBy = arguments.Skip(2).Take(partitionEnd - 2)
                                 .Select(e => _sqlExpressionFactory.ApplyDefaultTypeMapping(e))
                                 .ToList();

      return new WindowFunctionExpression("NTILE",
                                          false,
                                          typeof(int),
                                          _typeMappingSource.FindMapping(typeof(int), _model),
                                          new[] { bucketCount },
                                          partitionBy,
                                          orderings?.Orderings);
   }
}
