using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Evaluatable expression filter decorator.
   /// </summary>
   /// <typeparam name="T">Type of inner filter.</typeparam>
   public sealed class CompositeEvaluatableExpressionFilter<T> : IEvaluatableExpressionFilter
      where T : class, IEvaluatableExpressionFilter
   {
      private readonly T _filter;
      private readonly IReadOnlyList<IEvaluatableExpressionFilterPlugin> _plugins;

      /// <summary>
      /// Initializes new instance of <see cref="CompositeEvaluatableExpressionFilter{T}"/>.
      /// </summary>
      /// <param name="filter">Inner filter.</param>
      /// <param name="plugins">Plugins.</param>
      public CompositeEvaluatableExpressionFilter(T filter, IEnumerable<IEvaluatableExpressionFilterPlugin> plugins)
      {
         _filter = filter ?? throw new ArgumentNullException(nameof(filter));
         _plugins = plugins?.ToList() ?? throw new ArgumentNullException(nameof(plugins));
      }

      /// <inheritdoc />
      public bool IsEvaluatableExpression(Expression expression, IModel model)
      {
         for (var i = 0; i < _plugins.Count; i++)
         {
            var isEvaluatable = _plugins[i].IsEvaluatableExpression(expression, model);
            if (isEvaluatable.HasValue)
               return isEvaluatable.Value;
         }

         return _filter.IsEvaluatableExpression(expression, model);
      }
   }
}
