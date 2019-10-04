using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.Expressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "Descending".
   /// </summary>
   public class DescendingTranslator : IMethodCallTranslator
   {
      private static readonly MethodInfo _descendingMethodInfo = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Descending), BindingFlags.Public | BindingFlags.Static);

      private readonly ISqlExpressionFactory _expressionFactory;

      /// <summary>
      /// Initializes new instance of <see cref="DescendingTranslator"/>.
      /// </summary>
      /// <param name="expressionFactory">Expression factory.</param>
      public DescendingTranslator([NotNull] ISqlExpressionFactory expressionFactory)
      {
         _expressionFactory = expressionFactory ?? throw new ArgumentNullException(nameof(expressionFactory));
      }

      /// <inheritdoc />
      [CanBeNull]
      public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
      {
         if (method == _descendingMethodInfo)
         {
            var column = arguments[1];
            return new DescendingExpression(_expressionFactory, column);
         }

         return null;
      }
   }
}
