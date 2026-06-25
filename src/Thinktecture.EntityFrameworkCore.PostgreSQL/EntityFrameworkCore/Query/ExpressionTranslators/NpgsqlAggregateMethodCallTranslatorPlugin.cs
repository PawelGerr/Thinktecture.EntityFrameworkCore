using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Plugin for aggregate method call translators.
/// </summary>
public sealed class NpgsqlAggregateMethodCallTranslatorPlugin : IAggregateMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IAggregateMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlAggregateMethodCallTranslatorPlugin"/>.
   /// </summary>
   /// <param name="sqlExpressionFactory">The SQL expression factory (Npgsql provider).</param>
   /// <param name="typeMappingSource">The relational type mapping source.</param>
   public NpgsqlAggregateMethodCallTranslatorPlugin(
      ISqlExpressionFactory sqlExpressionFactory,
      IRelationalTypeMappingSource typeMappingSource)
   {
      ArgumentNullException.ThrowIfNull(sqlExpressionFactory);
      ArgumentNullException.ThrowIfNull(typeMappingSource);

      // AggregateFunction is defined on the concrete Npgsql factory; on the Npgsql provider
      // ISqlExpressionFactory always resolves to NpgsqlSqlExpressionFactory.
      var npgsqlSqlExpressionFactory = (NpgsqlSqlExpressionFactory)sqlExpressionFactory;

      Translators =
      [
         new NpgsqlUuidAggregateMethodCallTranslator(npgsqlSqlExpressionFactory, typeMappingSource),
         new NpgsqlBitwiseAggregateMethodCallTranslator(npgsqlSqlExpressionFactory)
      ];
   }
}
