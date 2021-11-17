using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends the capabilities of <see cref="Microsoft.EntityFrameworkCore.Sqlite.Query.Internal.SqliteQueryableMethodTranslatingExpressionVisitor"/>.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor
      : SqliteQueryableMethodTranslatingExpressionVisitor
   {
      private readonly IRelationalTypeMappingSource _typeMappingSource;
      private readonly TableHintContextFactory _tableHintContextFactory;
      private readonly TempTableQueryContextFactory _tempTableQueryContextFactory;

      /// <inheritdoc />
      public ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(
         QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
         RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
         QueryCompilationContext queryCompilationContext,
         IRelationalTypeMappingSource typeMappingSource)
         : base(dependencies, relationalDependencies, queryCompilationContext)
      {
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
         _tableHintContextFactory = new TableHintContextFactory();

         var querySplittingBehavior = ((RelationalQueryCompilationContext)QueryCompilationContext).QuerySplittingBehavior;
         _tempTableQueryContextFactory = new TempTableQueryContextFactory(querySplittingBehavior);
      }

      /// <inheritdoc />
      protected ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(
         ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor parentVisitor,
         IRelationalTypeMappingSource typeMappingSource)
         : base(parentVisitor)
      {
         _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
         _tableHintContextFactory = parentVisitor._tableHintContextFactory;
         _tempTableQueryContextFactory = parentVisitor._tempTableQueryContextFactory;
      }

      /// <inheritdoc />
      protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
      {
         return new ThinktectureSqliteQueryableMethodTranslatingExpressionVisitor(this, _typeMappingSource);
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
      {
         return this.TranslateRelationalMethods(methodCallExpression, QueryCompilationContext, _tableHintContextFactory) ??
                this.TranslateBulkMethods(methodCallExpression, _typeMappingSource, QueryCompilationContext, _tempTableQueryContextFactory) ??
                base.VisitMethodCall(methodCallExpression);
      }
   }
}
