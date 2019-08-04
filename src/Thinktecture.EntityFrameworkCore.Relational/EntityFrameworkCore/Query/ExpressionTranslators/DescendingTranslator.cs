using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.Query.Expressions;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Translated extension method "Descending".
   /// </summary>
   public class DescendingTranslator : IMethodCallTranslator
   {
      private static readonly MethodInfo _descendingMethodInfo = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Descending), BindingFlags.Public | BindingFlags.Static);

      /// <inheritdoc />
      [CanBeNull]
      public Expression Translate(MethodCallExpression methodCallExpression)
      {
         if (methodCallExpression.Method == _descendingMethodInfo)
         {
            var column = methodCallExpression.Arguments[1];
            return new DescendingExpression(column);
         }

         return null;
      }
   }
}
