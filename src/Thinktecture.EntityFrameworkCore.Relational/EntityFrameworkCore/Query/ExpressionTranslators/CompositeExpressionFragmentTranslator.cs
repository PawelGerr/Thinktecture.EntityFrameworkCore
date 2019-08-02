using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Injects all registered <see cref="IExpressionFragmentTranslatorPlugin"/> and merged all their translators.
   /// </summary>
   public sealed class CompositeExpressionFragmentTranslator<T> : IExpressionFragmentTranslator
      where T : class, IExpressionFragmentTranslator
   {
      private readonly T _innerTranslator;
      private readonly List<IExpressionFragmentTranslator> _translators;

      /// <inheritdoc />
      public CompositeExpressionFragmentTranslator([NotNull] T innerTranslator,
                                                   [NotNull] IEnumerable<IExpressionFragmentTranslatorPlugin> plugins)
      {
         if (plugins == null)
            throw new ArgumentNullException(nameof(plugins));

         _innerTranslator = innerTranslator ?? throw new ArgumentNullException(nameof(innerTranslator));
         _translators = new List<IExpressionFragmentTranslator>(plugins.SelectMany(p => p.Translators));
      }

      /// <inheritdoc />
      [CanBeNull]
      public Expression Translate(Expression expression)
      {
         foreach (var translator in _translators)
         {
            var translatedExpression = translator.Translate(expression);

            if (translatedExpression != null)
               return translatedExpression;
         }

         return _innerTranslator.Translate(expression);
      }
   }
}
