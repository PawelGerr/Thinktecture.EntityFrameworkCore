using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Registers Thinktecture method translators for MySQL.
/// </summary>
public sealed class MySqlMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlMethodCallTranslatorPlugin" />.
   /// </summary>
   /// <param name="options">Relational options of the extension.</param>
   /// <param name="sqlExpressionFactory">SQL expression factory.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   /// <param name="model">The model.</param>
   public MySqlMethodCallTranslatorPlugin(
      RelationalDbContextOptionsExtensionOptions options,
      ISqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource,
      IModel model)
   {
      ArgumentNullException.ThrowIfNull(options);

      Translators = options.WindowFunctionsSupportEnabled
                       ? new IMethodCallTranslator[] { new MySqlDbFunctionsTranslator(sqlExpressionFactory, typeMappingSource, model) }
                       : Array.Empty<IMethodCallTranslator>();
   }
}