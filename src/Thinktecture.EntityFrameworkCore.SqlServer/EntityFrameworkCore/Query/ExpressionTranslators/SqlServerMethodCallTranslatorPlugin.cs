using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
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
   public SqlServerMethodCallTranslatorPlugin(
      RelationalDbContextOptionsExtensionOptions options,
      ISqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource,
      IModel model)
   {
      ArgumentNullException.ThrowIfNull(options);

      var translators = new List<IMethodCallTranslator>();

      if (options.WindowFunctionsSupportEnabled)
         translators.Add(new SqlServerDbFunctionsTranslator(sqlExpressionFactory, typeMappingSource, model));

      Translators = translators;
   }
}
