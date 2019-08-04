using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
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
            var orderBy = ExtractParams(methodCallExpression.Arguments[1]);

            return new RowNumberExpression(Array.Empty<Expression>(), orderBy);
         }

         if (methodCallExpression.Method == _rowNumberWithPartitionByMethod)
         {
            var partitionBy = ExtractParams(methodCallExpression.Arguments[1]);
            var orderBy = ExtractParams(methodCallExpression.Arguments[2]);

            return new RowNumberExpression(partitionBy, orderBy);
         }

         return null;
      }

      [NotNull]
      private static IReadOnlyCollection<Expression> ExtractParams([NotNull] Expression parameter)
      {
         if (parameter is ConstantExpression constant)
         {
            if (constant.Value == null)
               throw new NotSupportedException("The value 'null' is not supported.");

            if (constant.Value is IReadOnlyCollection<Expression> collection)
               return collection;

            if (constant.Value is IEnumerable<Expression> enumerable)
               return enumerable.ToList();
         }

         if (parameter is NewArrayExpression array)
            return array.Expressions;

         return new[] { parameter };
      }
   }
}
