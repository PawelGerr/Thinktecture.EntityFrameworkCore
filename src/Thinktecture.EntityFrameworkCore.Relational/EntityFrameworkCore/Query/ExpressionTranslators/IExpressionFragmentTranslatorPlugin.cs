using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Represents a plugin for <see cref="IExpressionFragmentTranslator"/>.
   /// </summary>
   public interface IExpressionFragmentTranslatorPlugin
   {
      /// <summary>
      /// Gets the expression fragment translators.
      /// </summary>
      IEnumerable<IExpressionFragmentTranslator> Translators { get; }
   }
}