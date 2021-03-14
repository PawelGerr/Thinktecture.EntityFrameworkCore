using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqlServerQuerySqlGenerator : SqlServerQuerySqlGenerator
   {
      private readonly ITenantDatabaseProvider _databaseProvider;

      /// <inheritdoc />
      public ThinktectureSqlServerQuerySqlGenerator(
         QuerySqlGeneratorDependencies dependencies,
         ITenantDatabaseProvider databaseProvider)
         : base(dependencies)
      {
         _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
      }

      /// <inheritdoc />
      protected override Expression VisitExtension(Expression expression)
      {
         if (expression is TempTableExpression tempTableExpression)
         {
            VisitTempTable(tempTableExpression);
            return expression;
         }

         if (expression is TableWithHintsExpression tableWithHints)
         {
            VisitTableWithHints(tableWithHints);
            return expression;
         }

         return base.VisitExtension(expression);
      }

      private void VisitTableWithHints(TableWithHintsExpression tableWithHints)
      {
         Visit(tableWithHints.Table);

         if (tableWithHints.TableHints.Count == 0)
            return;

         Sql.Append(" ").Append("WITH (");

         for (var i = 0; i < tableWithHints.TableHints.Count; i++)
         {
            if (i != 0)
               Sql.Append(", ");

            Sql.Append(tableWithHints.TableHints[i].ToString()!);
         }

         Sql.Append(")");
      }

      private void VisitTempTable(TempTableExpression tempTableExpression)
      {
         Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableExpression.Name))
            .Append(AliasSeparator)
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(tempTableExpression.Alias));
      }

      /// <inheritdoc />
      protected override Expression VisitSelect(SelectExpression selectExpression)
      {
         if (selectExpression.TryGetDeleteExpression(out var deleteExpression))
            return GenerateDeleteStatement(selectExpression, deleteExpression);

         return base.VisitSelect(selectExpression);
      }

      private Expression GenerateDeleteStatement(SelectExpression selectExpression, DeleteExpression deleteExpression)
      {
         if (selectExpression.IsDistinct)
            throw new NotSupportedException("A DISTINCT clause is not supported in a DELETE statement.");

         if (selectExpression.Tables.Count == 0)
            throw new NotSupportedException("A DELETE statement without any tables is invalid.");

         if (selectExpression.GroupBy.Count > 0)
            throw new NotSupportedException("A GROUP BY clause is not supported in a DELETE statement.");

         if (selectExpression.Having is not null)
            throw new NotSupportedException("A HAVING clause is not supported in a DELETE statement.");

         if (selectExpression.Offset is not null)
            throw new NotSupportedException("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

         Sql.Append("DELETE ");

         GenerateTop(selectExpression);

         Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(deleteExpression.Table.Alias)).AppendLine()
            .Append("FROM ");

         for (var i = 0; i < selectExpression.Tables.Count; i++)
         {
            if (i > 0)
               Sql.AppendLine();

            Visit(selectExpression.Tables[i]);
         }

         if (selectExpression.Predicate is not null)
         {
            Sql.AppendLine().Append("WHERE ");
            Visit(selectExpression.Predicate);
         }

         Sql.Append(Dependencies.SqlGenerationHelper.StatementTerminator).AppendLine()
            .Append("SELECT @@ROWCOUNT").Append(Dependencies.SqlGenerationHelper.StatementTerminator);

         return selectExpression;
      }

      /// <inheritdoc />
      protected override Expression VisitTable(TableExpression tableExpression)
      {
         if (tableExpression == null)
            throw new ArgumentNullException(nameof(tableExpression));

         var databaseName = _databaseProvider.GetDatabaseName(tableExpression.Schema, tableExpression.Name);

         if (!String.IsNullOrWhiteSpace(databaseName))
         {
            Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName))
               .Append(String.IsNullOrWhiteSpace(tableExpression.Schema) ? ".." : ".");
         }

         var expression = base.VisitTable(tableExpression);

         return expression;
      }
   }
}
