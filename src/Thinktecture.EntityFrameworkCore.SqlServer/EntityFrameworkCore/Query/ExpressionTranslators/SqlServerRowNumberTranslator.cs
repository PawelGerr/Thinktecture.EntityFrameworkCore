using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.Query.Expressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "RowNumber"
   /// </summary>
   public class SqlServerRowNumberTranslator : IMethodCallTranslator
   {
      private static readonly MethodInfo _rowNumberWithPartitionByMethod = typeof(SqlServerDbFunctionsExtensions).GetMethod(nameof(SqlServerDbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object), typeof(object) });
      private static readonly MethodInfo _rowNumberMethod = typeof(SqlServerDbFunctionsExtensions).GetMethod(nameof(SqlServerDbFunctionsExtensions.RowNumber), new[] { typeof(DbFunctions), typeof(object) });

      /// <inheritdoc />
      [CanBeNull]
      public Expression Translate(MethodCallExpression methodCallExpression)
      {
         if (methodCallExpression.Method == _rowNumberMethod)
         {
            var orderByParams = ExtractParams(methodCallExpression.Arguments[1]);
            var orderBy = ConvertOrderBy(orderByParams);

            return new RowNumberExpression(Array.Empty<Expression>(), orderBy);
         }

         if (methodCallExpression.Method == _rowNumberWithPartitionByMethod)
         {
            var partitionBy = ExtractParams(methodCallExpression.Arguments[1]);
            var orderByParams = ExtractParams(methodCallExpression.Arguments[2]);
            var orderBy = ConvertOrderBy(orderByParams);

            return new RowNumberExpression(partitionBy, orderBy);
         }

         return null;
      }

      [NotNull]
      private static ReadOnlyCollection<Expression> ExtractParams([NotNull] Expression parameter)
      {
         if (parameter is ConstantExpression constant)
         {
            if (constant.Value == null)
               throw new NotSupportedException("The value 'null' is not supported.");

            if (constant.Value is IEnumerable<Expression> enumerable)
               return enumerable.ToList().AsReadOnly();
         }

         if (parameter is NewArrayExpression array)
            return array.Expressions;

         return new List<Expression> { parameter }.AsReadOnly();
      }

      [NotNull]
      private static IReadOnlyCollection<Expression> ConvertOrderBy([NotNull] IEnumerable<Expression> orderByParams)
      {
         return orderByParams.Select(ConvertOrderBy).ToList().AsReadOnly();
      }

      [NotNull]
      private static Expression ConvertOrderBy([NotNull] Expression expression)
      {
         if (expression is DescendingExpression)
            return expression;

         return ExtractOrderBy(expression);
      }

      [NotNull]
      private static Expression ExtractOrderBy([NotNull] Expression expression)
      {
         while (true)
         {
            if (expression is ColumnExpression || expression is DescendingExpression)
               return expression;

            if (expression.NodeType == ExpressionType.Convert)
            {
               expression = ((UnaryExpression)expression).Operand;
            }
            else
            {
               throw new ArgumentException($"Unexpected 'order by' expression. Type: {expression.GetType().DisplayName()}.");
            }
         }
      }
   }
}
