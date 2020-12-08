using System;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Factory for creation of the <see cref="ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public sealed class RelationalQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
   {
      private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
      private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;

      /// <summary>
      /// Initializes new instance of <see cref="RelationalQueryableMethodTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="relationalDependencies">Relational dependencies.</param>
      public RelationalQueryableMethodTranslatingExpressionVisitorFactory(QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                         RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
         _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      }

      /// <inheritdoc />
      public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
      {
         return new ThinktectureRelationalQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, queryCompilationContext);
      }
   }
}
