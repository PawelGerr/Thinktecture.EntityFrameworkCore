using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Base class.
/// </summary>
public abstract class DefaultSqlExpressionVisitor : SqlExpressionVisitor
{
   /// <inheritdoc />
   protected override Expression VisitCase(CaseExpression caseExpression)
   {
      var whenClauses = new List<CaseWhenClause>();

      for (var i = 0; i < caseExpression.WhenClauses.Count; i++)
      {
         var whenClause = caseExpression.WhenClauses[i];
         var test = (SqlExpression)Visit(whenClause.Test);
         var result = (SqlExpression)Visit(whenClause.Result);
         whenClauses.Add(new CaseWhenClause(test, result));
      }

      return caseExpression.Update((SqlExpression?)Visit(caseExpression.Operand),
                                   whenClauses,
                                   (SqlExpression?)Visit(caseExpression.ElseResult));
   }

   /// <inheritdoc />
   protected override Expression VisitCollate(CollateExpression collateExpression)
   {
      return collateExpression.Update((SqlExpression)Visit(collateExpression.Operand));
   }

   /// <inheritdoc />
   protected override Expression VisitColumn(ColumnExpression columnExpression)
   {
      return columnExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitDistinct(DistinctExpression distinctExpression)
   {
      return distinctExpression.Update((SqlExpression)Visit(distinctExpression.Operand));
   }

   /// <inheritdoc />
   protected override Expression VisitExists(ExistsExpression existsExpression)
   {
      return existsExpression.Update((SelectExpression)Visit(existsExpression.Subquery));
   }

   /// <inheritdoc />
   protected override Expression VisitFromSql(FromSqlExpression fromSqlExpression)
   {
      return fromSqlExpression.Update(Visit(fromSqlExpression.Arguments));
   }

   /// <inheritdoc />
   protected override Expression VisitIn(InExpression inExpression)
   {
      return inExpression.Update((SqlExpression)Visit(inExpression.Item),
                                 (SqlExpression?)Visit(inExpression.Values),
                                 (SelectExpression?)Visit(inExpression.Subquery));
   }

   /// <inheritdoc />
   protected override Expression VisitLike(LikeExpression likeExpression)
   {
      return likeExpression.Update((SqlExpression)Visit(likeExpression.Match),
                                   (SqlExpression)Visit(likeExpression.Pattern),
                                   (SqlExpression?)Visit(likeExpression.EscapeChar));
   }

   /// <inheritdoc />
   // ReSharper disable PossibleUnintendedReferenceComparison
   protected override Expression VisitSelect(SelectExpression selectExpression)
   {
      var changed = false;
      var projections = new List<ProjectionExpression>();

      for (var i = 0; i < selectExpression.Projection.Count; i++)
      {
         var item = selectExpression.Projection[i];
         var updatedProjection = (ProjectionExpression)Visit(item);
         projections.Add(updatedProjection);
         changed |= updatedProjection != item;
      }

      var tables = new List<TableExpressionBase>();

      for (var i = 0; i < selectExpression.Tables.Count; i++)
      {
         var table = selectExpression.Tables[i];
         var newTable = VisitTableExpressionBase(table);
         changed |= newTable != table;
         tables.Add(newTable);
      }

      var predicate = (SqlExpression?)Visit(selectExpression.Predicate);
      changed |= predicate != selectExpression.Predicate;

      var groupBy = new List<SqlExpression>();

      for (var i = 0; i < selectExpression.GroupBy.Count; i++)
      {
         var groupingKey = selectExpression.GroupBy[i];
         var newGroupingKey = (SqlExpression)Visit(groupingKey);
         changed |= newGroupingKey != groupingKey;
         groupBy.Add(newGroupingKey);
      }

      var havingExpression = (SqlExpression?)Visit(selectExpression.Having);
      changed |= havingExpression != selectExpression.Having;

      var orderings = new List<OrderingExpression>();

      for (var i = 0; i < selectExpression.Orderings.Count; i++)
      {
         var ordering = selectExpression.Orderings[i];
         var orderingExpression = (SqlExpression)Visit(ordering.Expression);
         changed |= orderingExpression != ordering.Expression;
         orderings.Add(ordering.Update(orderingExpression));
      }

      var offset = (SqlExpression?)Visit(selectExpression.Offset);
      changed |= offset != selectExpression.Offset;

      var limit = (SqlExpression?)Visit(selectExpression.Limit);
      changed |= limit != selectExpression.Limit;

      return changed
                ? selectExpression.Update(projections, tables, predicate, groupBy, havingExpression, orderings, limit, offset)
                : selectExpression;
   }

   /// <summary>
   /// Visits a <see cref="TableExpressionBase"/> of the <see cref="SelectExpression"/>.
   /// </summary>
   /// <param name="table">Table to visit.</param>
   /// <returns>Visited table.</returns>
   protected virtual TableExpressionBase VisitTableExpressionBase(TableExpressionBase table)
   {
      return (TableExpressionBase)Visit(table);
   }

   // ReSharper restore PossibleUnintendedReferenceComparison

   /// <inheritdoc />
   protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
   {
      return sqlBinaryExpression.Update((SqlExpression)Visit(sqlBinaryExpression.Left),
                                        (SqlExpression)Visit(sqlBinaryExpression.Right));
   }

   /// <inheritdoc />
   protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
   {
      return sqlUnaryExpression.Update((SqlExpression)Visit(sqlUnaryExpression.Operand));
   }

   /// <inheritdoc />
   protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstantExpression)
   {
      return sqlConstantExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression)
   {
      return sqlFragmentExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
   {
      SqlExpression[]? arguments = null;

      if (sqlFunctionExpression.Arguments is not null)
      {
         arguments = new SqlExpression[sqlFunctionExpression.Arguments.Count];

         for (var i = 0; i < arguments.Length; i++)
         {
            arguments[i] = (SqlExpression)Visit(sqlFunctionExpression.Arguments[i]);
         }
      }

      return sqlFunctionExpression.Update((SqlExpression?)Visit(sqlFunctionExpression.Instance), arguments);
   }

   /// <inheritdoc />
   protected override Expression VisitTableValuedFunction(TableValuedFunctionExpression tableValuedFunctionExpression)
   {
      var arguments = new SqlExpression[tableValuedFunctionExpression.Arguments.Count];

      for (var i = 0; i < arguments.Length; i++)
      {
         arguments[i] = (SqlExpression)Visit(tableValuedFunctionExpression.Arguments[i]);
      }

      return tableValuedFunctionExpression.Update(arguments);
   }

   /// <inheritdoc />
   protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
   {
      return sqlParameterExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitTable(TableExpression tableExpression)
   {
      return tableExpression;
   }

   /// <inheritdoc />
   protected override Expression VisitProjection(ProjectionExpression projectionExpression)
   {
      return projectionExpression.Update((SqlExpression)Visit(projectionExpression.Expression));
   }

   /// <inheritdoc />
   protected override Expression VisitOrdering(OrderingExpression orderingExpression)
   {
      return orderingExpression.Update((SqlExpression)Visit(orderingExpression.Expression));
   }

   /// <inheritdoc />
   protected override Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression)
   {
      return crossJoinExpression.Update(VisitTableExpressionBase(crossJoinExpression.Table));
   }

   /// <inheritdoc />
   protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
   {
      return crossApplyExpression.Update(VisitTableExpressionBase(crossApplyExpression.Table));
   }

   /// <inheritdoc />
   protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
   {
      return outerApplyExpression.Update(VisitTableExpressionBase(outerApplyExpression.Table));
   }

   /// <inheritdoc />
   protected override Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression)
   {
      return innerJoinExpression.Update(VisitTableExpressionBase(innerJoinExpression.Table),
                                        (SqlExpression)Visit(innerJoinExpression.JoinPredicate));
   }

   /// <inheritdoc />
   protected override Expression VisitLeftJoin(LeftJoinExpression leftJoinExpression)
   {
      return leftJoinExpression.Update(VisitTableExpressionBase(leftJoinExpression.Table),
                                       (SqlExpression)Visit(leftJoinExpression.JoinPredicate));
   }

   /// <inheritdoc />
   protected override Expression VisitScalarSubquery(ScalarSubqueryExpression scalarSubqueryExpression)
   {
      return scalarSubqueryExpression.Update((SelectExpression)Visit(scalarSubqueryExpression.Subquery));
   }

   /// <inheritdoc />
   protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
   {
      var partitions = new List<SqlExpression>();

      for (var i = 0; i < rowNumberExpression.Partitions.Count; i++)
      {
         var partition = rowNumberExpression.Partitions[i];
         var newPartition = (SqlExpression)Visit(partition);
         partitions.Add(newPartition);
      }

      var orderings = new List<OrderingExpression>();

      for (var i = 0; i < rowNumberExpression.Orderings.Count; i++)
      {
         var ordering = rowNumberExpression.Orderings[i];
         var newOrdering = (OrderingExpression)Visit(ordering);
         orderings.Add(newOrdering);
      }

      return rowNumberExpression.Update(partitions, orderings);
   }

   /// <inheritdoc />
   protected override Expression VisitExcept(ExceptExpression exceptExpression)
   {
      return exceptExpression.Update((SelectExpression)Visit(exceptExpression.Source1),
                                     (SelectExpression)Visit(exceptExpression.Source2));
   }

   /// <inheritdoc />
   protected override Expression VisitIntersect(IntersectExpression intersectExpression)
   {
      return intersectExpression.Update((SelectExpression)Visit(intersectExpression.Source1),
                                        (SelectExpression)Visit(intersectExpression.Source2));
   }

   /// <inheritdoc />
   protected override Expression VisitUnion(UnionExpression unionExpression)
   {
      return unionExpression.Update((SelectExpression)Visit(unionExpression.Source1),
                                    (SelectExpression)Visit(unionExpression.Source2));
   }

   /// <inheritdoc />
   protected override Expression VisitAtTimeZone(AtTimeZoneExpression atTimeZoneExpression)
   {
      return atTimeZoneExpression.Update((SqlExpression)Visit(atTimeZoneExpression.Operand),
                                         (SqlExpression)Visit(atTimeZoneExpression.TimeZone));
   }
}
