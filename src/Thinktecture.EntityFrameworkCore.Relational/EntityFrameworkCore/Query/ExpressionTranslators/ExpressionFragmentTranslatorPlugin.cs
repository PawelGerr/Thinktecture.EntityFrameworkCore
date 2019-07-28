using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   internal class ExpressionFragmentTranslatorPlugin : IExpressionFragmentTranslatorPlugin
   {
      public IEnumerable<IExpressionFragmentTranslator> Translators { get; }

      public ExpressionFragmentTranslatorPlugin([NotNull] IEnumerable<IExpressionFragmentTranslator> translators)
      {
         Translators = translators ?? throw new ArgumentNullException(nameof(translators));
      }
   }
}
