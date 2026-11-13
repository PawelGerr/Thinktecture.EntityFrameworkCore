using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Registers MySQL aggregate method translators.
/// </summary>
public sealed class MySqlAggregateMethodCallTranslatorPlugin : IAggregateMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IAggregateMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlAggregateMethodCallTranslatorPlugin" />.
   /// </summary>
   /// <param name="sqlExpressionFactory">SQL expression factory.</param>
   public MySqlAggregateMethodCallTranslatorPlugin(ISqlExpressionFactory sqlExpressionFactory)
   {
      ArgumentNullException.ThrowIfNull(sqlExpressionFactory);
      Translators = [new MySqlBitwiseAggregateMethodCallTranslator(sqlExpressionFactory)];
   }
}
