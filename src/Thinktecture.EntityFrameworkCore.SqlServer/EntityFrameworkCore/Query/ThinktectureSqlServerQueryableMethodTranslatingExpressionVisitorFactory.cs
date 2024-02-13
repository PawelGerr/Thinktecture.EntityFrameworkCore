using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Factory for creation of the <see cref="ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory
   : IQueryableMethodTranslatingExpressionVisitorFactory
{
   private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
   private readonly RelationalQueryableMethodTranslatingExpressionVisitorDependencies _relationalDependencies;
   private readonly ISqlServerSingletonOptions _sqlServerSingletonOptions;

   /// <summary>
   /// Initializes new instance of <see cref="ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory"/>.
   /// </summary>
   /// <param name="dependencies">Dependencies.</param>
   /// <param name="relationalDependencies">Relational dependencies.</param>
   /// <param name="sqlServerSingletonOptions">Options.</param>
   public ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory(
      QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
      RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
      ISqlServerSingletonOptions sqlServerSingletonOptions)
   {
      _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
      _relationalDependencies = relationalDependencies ?? throw new ArgumentNullException(nameof(relationalDependencies));
      _sqlServerSingletonOptions = sqlServerSingletonOptions;
   }

   /// <inheritdoc />
   public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
   {
      return new ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor(_dependencies, _relationalDependencies, (SqlServerQueryCompilationContext)queryCompilationContext, _sqlServerSingletonOptions);
   }
}
