using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Factory for creation of the <see cref="ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public sealed class ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
   {
      private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
      private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;

      /// <summary>
      /// Initializes new instance of <see cref="ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="relationalDependencies">Relational dependencies.</param>
      public ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory(QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
         _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      }

      /// <inheritdoc />
      public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
      {
         return new ThinktectureSqliteRelationalQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, queryCompilationContext);
      }
   }
}
