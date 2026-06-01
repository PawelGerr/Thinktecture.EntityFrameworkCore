using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Plugin registering method translators for SQLite.
/// </summary>
public sealed class SqliteMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteMethodCallTranslatorPlugin"/>.
   /// </summary>
   public SqliteMethodCallTranslatorPlugin(
      RelationalDbContextOptionsExtensionOptions options,
      ISqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource,
      IModel model)
   {
      ArgumentNullException.ThrowIfNull(options);

      var translators = new List<IMethodCallTranslator>();

      if (options.WindowFunctionsSupportEnabled)
         translators.Add(new SqliteDbFunctionsTranslator(sqlExpressionFactory, typeMappingSource, model));

      Translators = translators;
   }
}
