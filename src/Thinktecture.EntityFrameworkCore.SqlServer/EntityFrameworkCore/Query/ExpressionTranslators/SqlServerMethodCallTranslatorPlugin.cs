using Microsoft.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Plugin registering method translators.
/// </summary>
public sealed class SqlServerMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerMethodCallTranslatorPlugin"/>.
   /// </summary>
   public SqlServerMethodCallTranslatorPlugin(RelationalDbContextOptionsExtensionOptions options, ISqlExpressionFactory sqlExpressionFactory)
   {
      ArgumentNullException.ThrowIfNull(options);

      var translators = new List<IMethodCallTranslator>();

      if (options.WindowFunctionsSupportEnabled)
         translators.Add(new SqlServerDbFunctionsTranslator(sqlExpressionFactory));

      Translators = translators;
   }
}
