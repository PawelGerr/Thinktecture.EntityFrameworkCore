using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
#pragma warning disable CA1812
   // ReSharper disable once ClassNeverInstantiated.Global
   internal sealed class ExpressionFragmentTranslatorPlugin<TTranslator> : IExpressionFragmentTranslatorPlugin
      where TTranslator : class, IExpressionFragmentTranslator
   {
      public IEnumerable<IExpressionFragmentTranslator> Translators { get; }

      public ExpressionFragmentTranslatorPlugin([NotNull] TTranslator translator)
      {
         if (translator == null)
            throw new ArgumentNullException(nameof(translator));

         Translators = new[] { translator };
      }
   }
}
