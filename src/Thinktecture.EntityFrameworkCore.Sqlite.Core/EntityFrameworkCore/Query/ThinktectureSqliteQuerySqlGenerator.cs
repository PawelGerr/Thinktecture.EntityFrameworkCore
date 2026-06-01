using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqliteQuerySqlGenerator : SqliteQuerySqlGenerator
{
   /// <inheritdoc />
   public ThinktectureSqliteQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies)
      : base(dependencies)
   {
   }

   /// <inheritdoc />
   protected override Expression VisitExtension(Expression extensionExpression)
   {
      return extensionExpression switch
      {
         WindowFunctionExpression windowFunctionExpression => VisitWindowFunction(windowFunctionExpression),
         _ => base.VisitExtension(extensionExpression)
      };
   }

   private Expression VisitWindowFunction(WindowFunctionExpression windowFunctionExpression)
   {
      Sql.Append(windowFunctionExpression.Name).Append(" (");

      for (var i = 0; i < windowFunctionExpression.Arguments.Count; i++)
      {
         if (i != 0)
            Sql.Append(", ");

         Visit(windowFunctionExpression.Arguments[i]);
      }

      if (windowFunctionExpression.Arguments.Count == 0 && windowFunctionExpression.UseAsteriskWhenNoArguments)
         Sql.Append("*");

      Sql.Append(") OVER (");

      if (windowFunctionExpression.Partitions.Count != 0)
      {
         Sql.Append("PARTITION BY ");

         for (var i = 0; i < windowFunctionExpression.Partitions.Count; i++)
         {
            if (i != 0)
               Sql.Append(", ");

            Visit(windowFunctionExpression.Partitions[i]);
         }
      }

      if (windowFunctionExpression.Orderings.Count != 0)
      {
         Sql.Append(" ORDER BY ");

         for (var i = 0; i < windowFunctionExpression.Orderings.Count; i++)
         {
            if (i != 0)
               Sql.Append(", ");

            VisitOrdering(windowFunctionExpression.Orderings[i]);
         }
      }

      Sql.Append(")");

      return windowFunctionExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitTable(TableExpression tableExpression)
   {
      ArgumentNullException.ThrowIfNull(tableExpression);

      var tempTable = tableExpression.FindAnnotation(ThinktectureBulkOperationsAnnotationNames.TEMP_TABLE);

      if (tempTable is not null)
      {
         var tempTableName = (string?)tempTable.Value ?? throw new Exception("Temp table name cannot be null.");
         Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableName))
            .Append(AliasSeparator)
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

         return tableExpression;
      }

      return base.VisitTable(tableExpression);
   }
}
