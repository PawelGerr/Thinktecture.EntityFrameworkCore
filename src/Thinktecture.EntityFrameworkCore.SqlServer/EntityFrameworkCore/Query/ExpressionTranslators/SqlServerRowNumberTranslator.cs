using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "RowNumber"
   /// </summary>
   public sealed class SqlServerRowNumberTranslator : IMethodCallTranslator
   {
      /// <inheritdoc />
      public SqlExpression? Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
      {
         if (method.DeclaringType == typeof(SqlServerDbFunctionsExtensions))
         {
            switch (method.Name)
            {
               case nameof(SqlServerDbFunctionsExtensions.OrderBy):
               {
                  var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, true)).ToList();
                  return new RowNumberClauseOrderingsExpression(orderBy);
               }
               case nameof(SqlServerDbFunctionsExtensions.OrderByDescending):
               {
                  var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, false)).ToList();
                  return new RowNumberClauseOrderingsExpression(orderBy);
               }
               case nameof(SqlServerDbFunctionsExtensions.ThenBy):
               {
                  var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, true));
                  return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
               }
               case nameof(SqlServerDbFunctionsExtensions.ThenByDescending):
               {
                  var orderBy = arguments.Skip(1).Select(e => new OrderingExpression(e, false));
                  return ((RowNumberClauseOrderingsExpression)arguments[0]).AddColumns(orderBy);
               }
               case nameof(SqlServerDbFunctionsExtensions.RowNumber):
               {
                  var partitionBy = arguments.Skip(1).Take(arguments.Count - 2).ToList();
                  var orderings = (RowNumberClauseOrderingsExpression)arguments[^1];
                  return new RowNumberExpression(partitionBy, orderings.Orderings, RelationalTypeMapping.NullMapping);
               }
               default:
                  throw new InvalidOperationException($"Unexpected method '{method.Name}' in '{nameof(SqlServerDbFunctionsExtensions)}'.");
            }
         }

         return null;
      }
   }
}
