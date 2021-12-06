using System;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Factory for creation of the <see cref="ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
public sealed class ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
{
   private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
   private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="relationalDependencies">Relational dependencies.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   public ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      IRelationalTypeMappingSource typeMappingSource)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
   {
      return new ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, queryCompilationContext, _typeMappingSource);
   }
}