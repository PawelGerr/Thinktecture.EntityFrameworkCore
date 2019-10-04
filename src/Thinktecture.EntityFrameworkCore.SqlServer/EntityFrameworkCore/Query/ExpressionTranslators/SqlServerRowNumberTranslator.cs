using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "RowNumber"
   /// </summary>
   public class SqlServerRowNumberTranslator : IMethodCallTranslator
   {
      private static readonly MethodInfo _rowNumberWithPartitionByMethod = typeof(SqlServerDbFunctionsExtensions).GetMethod(nameof(SqlServerDbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object), typeof(object) });
      private static readonly MethodInfo _rowNumberMethod = typeof(SqlServerDbFunctionsExtensions).GetMethod(nameof(SqlServerDbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object) });

      private readonly ISqlExpressionFactory _expressionFactory;

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerRowNumberTranslator"/>.
      /// </summary>
      /// <param name="expressionFactory">Expression factory.</param>
      public SqlServerRowNumberTranslator([NotNull] ISqlExpressionFactory expressionFactory)
      {
         _expressionFactory = expressionFactory ?? throw new ArgumentNullException(nameof(expressionFactory));
      }

      /// <inheritdoc />
      [CanBeNull]
      public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
      {
         if (method == _rowNumberMethod)
         {
            var orderBy = ExtractParams(arguments[1]).Select(e => new OrderingExpression(e, true)).ToList();

            return new RowNumberExpression(Array.Empty<SqlExpression>(), orderBy, RelationalTypeMapping.NullMapping);
         }

         if (method == _rowNumberWithPartitionByMethod)
         {
            var partitionBy = ExtractParams(arguments[1]);
            var orderBy = ExtractParams(arguments[2]).Select(e => new OrderingExpression(e, true)).ToList();

            return new RowNumberExpression(partitionBy, orderBy, RelationalTypeMapping.NullMapping);
         }

         return null;
      }

      [NotNull]
      private static IReadOnlyList<SqlExpression> ExtractParams([NotNull] SqlExpression parameter)
      {
         if (parameter is SqlUnaryExpression unary)
         {
            if (unary.OperatorType == ExpressionType.Convert)
               return new[] { unary.Operand };
         }

         if (parameter is SqlConstantExpression constant)
         {
            if (constant.Value == null)
               throw new NotSupportedException("The value 'null' is not supported.");

            if (constant.Value is IReadOnlyList<SqlExpression> collection)
               return collection;

            if (constant.Value is IEnumerable<SqlExpression> enumerable)
               return enumerable.ToList();
         }

         return new[] { parameter };
      }
   }
}
