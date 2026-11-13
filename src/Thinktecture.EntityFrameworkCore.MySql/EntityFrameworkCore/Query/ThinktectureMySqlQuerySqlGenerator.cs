using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Extends the MySQL SQL generator with Thinktecture expressions.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class ThinktectureMySqlQuerySqlGenerator : MySqlQuerySqlGenerator
{
   /// <summary>
   /// Initializes a new instance of <see cref="ThinktectureMySqlQuerySqlGenerator" />.
   /// </summary>
   /// <param name="dependencies">Generator dependencies.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   /// <param name="mySqlOptions">MySQL provider options.</param>
   public ThinktectureMySqlQuerySqlGenerator(
      QuerySqlGeneratorDependencies dependencies,
      IRelationalTypeMappingSource typeMappingSource,
      IMySqlOptions mySqlOptions)
      : base(dependencies, typeMappingSource, mySqlOptions)
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

   /// <inheritdoc />
   protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
   {
      // ROW_NUMBER() is BIGINT UNSIGNED in MySQL, which can't be read as long.
      Sql.Append("CAST(");
      base.VisitRowNumber(rowNumberExpression);
      Sql.Append(" AS signed)");
      return rowNumberExpression;
   }

   private Expression VisitWindowFunction(WindowFunctionExpression windowFunctionExpression)
   {
      // MySQL rejects a space between the name and '(' for built-ins like COUNT/SUM/MAX.
      Sql.Append(windowFunctionExpression.Name).Append("(");

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

      if (tempTable is null)
         return base.VisitTable(tableExpression);

      var tempTableName = (string?)tempTable.Value ?? throw new InvalidOperationException("Temp table name cannot be null.");
      Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableName))
         .Append(AliasSeparator)
         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

      return tableExpression;
   }
}