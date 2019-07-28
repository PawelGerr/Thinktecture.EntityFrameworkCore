using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Injects all registered <see cref="IExpressionFragmentTranslatorPlugin"/> and merged all their translators.
   /// </summary>
   public sealed class CompositeExpressionFragmentTranslator : RelationalCompositeExpressionFragmentTranslator
   {
      /// <inheritdoc />
      public CompositeExpressionFragmentTranslator([NotNull] RelationalCompositeExpressionFragmentTranslatorDependencies dependencies,
                                                   [NotNull] IEnumerable<IExpressionFragmentTranslatorPlugin> plugins)
         : base(dependencies)
      {
         if (plugins == null)
            throw new ArgumentNullException(nameof(plugins));

         foreach (var plugin in plugins)
         {
            AddTranslators(plugin.Translators);
         }
      }
   }
}
