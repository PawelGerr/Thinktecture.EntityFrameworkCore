using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends <see cref="NpgsqlQuerySqlGenerator"/> to support Thinktecture extensions.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureNpgsqlQuerySqlGenerator : NpgsqlQuerySqlGenerator
{
   /// <inheritdoc />
   public ThinktectureNpgsqlQuerySqlGenerator(
      QuerySqlGeneratorDependencies dependencies,
      IRelationalTypeMappingSource typeMappingSource,
      INpgsqlSingletonOptions npgsqlSingletonOptions)
      : base(dependencies, typeMappingSource, npgsqlSingletonOptions.ReverseNullOrderingEnabled, npgsqlSingletonOptions.PostgresVersion)
   {
   }

   /// <inheritdoc />
   protected override Expression VisitExtension(Expression extensionExpression)
   {
      switch (extensionExpression)
      {
         case WindowFunctionExpression windowFunctionExpression:
            return VisitWindowFunction(windowFunctionExpression);
         default:
            return base.VisitExtension(extensionExpression);
      }
   }

   private Expression VisitWindowFunction(WindowFunctionExpression windowFunctionExpression)
   {
      Sql.Append(windowFunctionExpression.Name).Append(" (");

      for (var i = 0; i < windowFunctionExpression.Arguments.Count; i++)
      {
         if (i != 0)
            Sql.Append(", ");

         var argument = windowFunctionExpression.Arguments[i];
         Visit(argument);
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

            var partition = windowFunctionExpression.Partitions[i];
            Visit(partition);
         }
      }

      if (windowFunctionExpression.Orderings.Count != 0)
      {
         Sql.Append(" ORDER BY ");

         for (var i = 0; i < windowFunctionExpression.Orderings.Count; i++)
         {
            if (i != 0)
               Sql.Append(", ");

            var ordering = windowFunctionExpression.Orderings[i];
            VisitOrdering(ordering);
         }
      }

      Sql.Append(")");

      return windowFunctionExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitTable(TableExpression tableExpression)
   {
      ArgumentNullException.ThrowIfNull(tableExpression);

      return VisitTableOrTempTable(tableExpression);
   }

   private Expression VisitTableOrTempTable(TableExpression tableExpression)
   {
      var tempTable = tableExpression.FindAnnotation(ThinktectureBulkOperationsAnnotationNames.TEMP_TABLE);

      if (tempTable is not null)
      {
         var tempTableName = (string?)tempTable.Value ?? throw new Exception("Temp table name cannot be null.");
         Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableName))
            .Append(AliasSeparator)
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

         return tableExpression;
      }
      else
      {
         return base.VisitTable(tableExpression);
      }
   }
}
