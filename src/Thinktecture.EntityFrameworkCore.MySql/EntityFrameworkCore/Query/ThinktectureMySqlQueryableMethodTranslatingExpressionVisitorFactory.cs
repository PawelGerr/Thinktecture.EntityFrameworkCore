using System.Diagnostics.CodeAnalysis;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Creates MySQL queryable method translators with Thinktecture support.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
{
   private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
   private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
   private readonly IMySqlOptions _mySqlOptions;

   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlQueryableMethodTranslatingExpressionVisitorFactory" />.
   /// </summary>
   /// <param name="dependencies">Queryable translation dependencies.</param>
   /// <param name="relationalDependencies">Relational translation dependencies.</param>
   /// <param name="mySqlOptions">MySQL options.</param>
   public ThinktectureMySqlQueryableMethodTranslatingExpressionVisitorFactory(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      IMySqlOptions mySqlOptions)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      _mySqlOptions = mySqlOptions ?? throw new ArgumentNullException(nameof(mySqlOptions));
   }

   /// <inheritdoc />
   public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
   {
      return new ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor(
         _dependencies,
         _relationalDependencies,
         (RelationalQueryCompilationContext)queryCompilationContext,
         _mySqlOptions);
   }
}