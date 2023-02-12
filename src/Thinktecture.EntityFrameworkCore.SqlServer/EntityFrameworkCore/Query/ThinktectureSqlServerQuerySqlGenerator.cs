using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Internal;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <inheritdoc />
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public class ThinktectureSqlServerQuerySqlGenerator : SqlServerQuerySqlGenerator
{
   private readonly ITenantDatabaseProvider _databaseProvider;

   /// <inheritdoc />
   public ThinktectureSqlServerQuerySqlGenerator(
      QuerySqlGeneratorDependencies dependencies,
      IRelationalTypeMappingSource typeMappingSource,
      ITenantDatabaseProvider databaseProvider)
      : base(dependencies, typeMappingSource)
   {
      _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
   }

   /// <inheritdoc />
   protected override Expression VisitTable(TableExpression tableExpression)
   {
      ArgumentNullException.ThrowIfNull(tableExpression);

      var tableHints = tableExpression.FindAnnotation(ThinktectureRelationalAnnotationNames.TABLE_HINTS)?.Value as IReadOnlyList<ITableHint>;

      var visitedExpression = VisitTableOrTempTable(tableExpression);

      if (tableHints?.Count > 0)
      {
         Sql.Append(" ").Append("WITH (");

         for (var i = 0; i < tableHints.Count; i++)
         {
            if (i != 0)
               Sql.Append(", ");

            Sql.Append(tableHints[i].ToString(Dependencies.SqlGenerationHelper));
         }

         Sql.Append(")");
      }

      return visitedExpression;
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
         var databaseName = _databaseProvider.GetDatabaseName(tableExpression.Schema, tableExpression.Name);

         if (!String.IsNullOrWhiteSpace(databaseName))
         {
            Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName))
               .Append(String.IsNullOrWhiteSpace(tableExpression.Schema) ? ".." : ".");
         }

         return base.VisitTable(tableExpression);
      }
   }
}
