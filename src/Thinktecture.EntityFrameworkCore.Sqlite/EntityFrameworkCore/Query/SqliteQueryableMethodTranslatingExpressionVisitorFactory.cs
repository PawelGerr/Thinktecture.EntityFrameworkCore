using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Factory for creation of the <see cref="SqliteRelationalQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   public class SqliteQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
   {
      private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
      private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;

      /// <summary>
      /// Initializes new instance of <see cref="SqliteQueryableMethodTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      /// <param name="relationalDependencies">Relational dependencies.</param>
      public SqliteQueryableMethodTranslatingExpressionVisitorFactory([NotNull] QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
                                                                      [NotNull] RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
         _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      }

      /// <inheritdoc />
      [NotNull]
      public QueryableMethodTranslatingExpressionVisitor Create(IModel model)
      {
         return new SqliteRelationalQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, model);
      }
   }
}
