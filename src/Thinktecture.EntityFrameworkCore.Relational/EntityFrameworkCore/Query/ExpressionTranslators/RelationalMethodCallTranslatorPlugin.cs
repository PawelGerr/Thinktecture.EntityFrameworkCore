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
   public RelationalMethodCallTranslatorPlugin(RelationalDbContextOptionsExtension extension, ISqlExpressionFactory sqlExpressionFactory)
   {
      ArgumentNullException.ThrowIfNull(extension);

      var translators = new List<IMethodCallTranslator>();

      if (extension.AddRowNumberSupport)
         translators.Add(new RowNumberTranslator(sqlExpressionFactory));

      Translators = translators;
   }
}
