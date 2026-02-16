using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Factory for <see cref="ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitorFactory
   : IQueryableMethodTranslatingExpressionVisitorFactory
{
   private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
   private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
   private readonly INpgsqlSingletonOptions _npgsqlSingletonOptions;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="relationalDependencies">Relational dependencies.</param>
   /// <param name="npgsqlSingletonOptions">Options.</param>
   public ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitorFactory(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      INpgsqlSingletonOptions npgsqlSingletonOptions)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      _npgsqlSingletonOptions = npgsqlSingletonOptions;
   }

   /// <inheritdoc />
   public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
   {
      return new ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, (RelationalQueryCompilationContext)queryCompilationContext, _npgsqlSingletonOptions);
   }
}
