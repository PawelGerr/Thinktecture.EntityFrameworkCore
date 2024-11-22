using Microsoft.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Plugin registering method translators.
/// </summary>
public sealed class RelationalMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes new instance of <see cref="RelationalMethodCallTranslatorPlugin"/>.
   /// </summary>
   public RelationalMethodCallTranslatorPlugin(RelationalDbContextOptionsExtensionOptions options, ISqlExpressionFactory sqlExpressionFactory)
   {
      ArgumentNullException.ThrowIfNull(options);

      var translators = new List<IMethodCallTranslator>();

      if (options.WindowFunctionsSupportEnabled)
         translators.Add(new RelationalDbFunctionsTranslator(sqlExpressionFactory));

      Translators = translators;
   }
}
