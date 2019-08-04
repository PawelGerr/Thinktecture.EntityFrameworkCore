using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
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
      public RelationalMethodCallTranslatorPlugin([NotNull] RelationalDbContextOptionsExtension extension)
      {
         if (extension == null)
            throw new ArgumentNullException(nameof(extension));

         var translators = new List<IMethodCallTranslator>();

         if (extension.AddDescendingSupport)
            translators.Add(new DescendingTranslator());

         Translators = translators;
      }
   }
}
