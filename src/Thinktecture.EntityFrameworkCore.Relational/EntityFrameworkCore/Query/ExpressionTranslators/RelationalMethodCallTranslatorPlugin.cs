using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Plugin registering method translators.
   /// </summary>
   public class RelationalMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
   {
      /// <inheritdoc />
      public IEnumerable<IMethodCallTranslator> Translators { get; }

      /// <summary>
      /// Initializes new instance of <see cref="RelationalMethodCallTranslatorPlugin"/>.
      /// </summary>
      public RelationalMethodCallTranslatorPlugin([NotNull] ISqlExpressionFactory expressionFactor,
                                                  [NotNull] RelationalDbContextOptionsExtension extension)
      {
         if (expressionFactor == null)
            throw new ArgumentNullException(nameof(expressionFactor));
         if (extension == null)
            throw new ArgumentNullException(nameof(extension));

         var translators = new List<IMethodCallTranslator>();

         if (extension.AddDescendingSupport)
            translators.Add(new DescendingTranslator(expressionFactor));

         Translators = translators;
      }
   }
}
