using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   public class ThinktectureSqliteSqlTranslatingExpressionVisitorFactory : IRelationalSqlTranslatingExpressionVisitorFactory
   {
      private readonly RelationalSqlTranslatingExpressionVisitorDependencies _dependencies;

      /// <summary>
      /// Initializes a new instance of <see cref="ThinktectureSqliteSqlTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      public ThinktectureSqliteSqlTranslatingExpressionVisitorFactory(
         RelationalSqlTranslatingExpressionVisitorDependencies dependencies)
      {
         _dependencies = dependencies;
      }

      /// <inheritdoc />
      public virtual RelationalSqlTranslatingExpressionVisitor Create(
         IModel model,
         QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
      {
         return new ThinktectureSqliteSqlTranslatingExpressionVisitor(_dependencies, model, queryableMethodTranslatingExpressionVisitor);
      }
   }
}
