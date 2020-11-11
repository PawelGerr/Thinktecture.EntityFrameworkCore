using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class ThinktectureSqlServerSqlTranslatingExpressionVisitorFactory
      : IRelationalSqlTranslatingExpressionVisitorFactory
   {
      private readonly RelationalSqlTranslatingExpressionVisitorDependencies _dependencies;

      /// <summary>
      /// Initializes a new instance of <see cref="ThinktectureSqlServerSqlTranslatingExpressionVisitorFactory"/>.
      /// </summary>
      /// <param name="dependencies">Dependencies.</param>
      public ThinktectureSqlServerSqlTranslatingExpressionVisitorFactory(RelationalSqlTranslatingExpressionVisitorDependencies dependencies)
      {
         _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      }

      /// <inheritdoc />
      public RelationalSqlTranslatingExpressionVisitor Create(
         QueryCompilationContext queryCompilationContext,
         QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
      {
         return new ThinktectureSqlServerSqlTranslatingExpressionVisitor(_dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor);
      }
   }
}
