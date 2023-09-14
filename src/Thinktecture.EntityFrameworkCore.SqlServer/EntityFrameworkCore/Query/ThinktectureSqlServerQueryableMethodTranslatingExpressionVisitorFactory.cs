using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Factory for creation of the <see cref="ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
public sealed class ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory
   : IQueryableMethodTranslatingExpressionVisitorFactory
{
   private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
   private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
   private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="relationalDependencies">Relational dependencies.</param>
   /// <param name="sqlServerSingletonOptions">Options.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   public ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      ISqlServerSingletonOptions sqlServerSingletonOptions,
      IRelationalTypeMappingSource typeMappingSource)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      _sqlServerSingletonOptions = sqlServerSingletonOptions;
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
   {
      return new ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, queryCompilationContext, _sqlServerSingletonOptions, _typeMappingSource);
   }
}
