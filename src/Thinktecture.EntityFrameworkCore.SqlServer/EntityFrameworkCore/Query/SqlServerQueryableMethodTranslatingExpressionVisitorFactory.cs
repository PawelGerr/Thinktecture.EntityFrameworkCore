using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Factory for creation of the <see cref="RelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public class SqlServerQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
   {
      private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
      private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerQueryableMethodTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="relationalDependencies">Relational dependencies.</param>
      public SqlServerQueryableMethodTranslatingExpressionVisitorFactory([NotNull] QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                         [NotNull] RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
         _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      }

      /// <inheritdoc />
      [NotNull]
      public QueryableMethodTranslatingExpressionVisitor Create(IModel model)
      {
         return new RelationalQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, model);
      }
   }
}
