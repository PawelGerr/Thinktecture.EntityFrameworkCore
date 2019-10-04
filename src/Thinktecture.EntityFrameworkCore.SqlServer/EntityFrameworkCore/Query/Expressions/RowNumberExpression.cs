using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.Expressions
{
   /// <summary>
   /// Expression rendering "ROW_NUMBER()".
   /// </summary>
   public class RowNumberExpression : SqlExpression
   {
      private readonly ISqlExpressionFactory _expressionFactory;
      private readonly IReadOnlyList<SqlExpression> _partitionBy;
      private readonly IReadOnlyList<SqlExpression> _orderBy;

      /// <inheritdoc />
      public override bool CanReduce => false;

      /// <summary>
      /// Initializes new instance of <see cref="RowNumberExpression"/>.
      /// </summary>
      /// <param name="expressionFactory">Expression factory.</param>
      /// <param name="partitionBy">Columns to partition by.</param>
      /// <param name="orderBy">Columns to order by.</param>
      public RowNumberExpression([NotNull] ISqlExpressionFactory expressionFactory,
                                 [NotNull] IReadOnlyList<SqlExpression> partitionBy,
                                 [NotNull] IReadOnlyList<SqlExpression> orderBy)
         : base(typeof(long), RelationalTypeMapping.NullMapping)
      {
         _expressionFactory = expressionFactory ?? throw new ArgumentNullException(nameof(expressionFactory));
         _partitionBy = partitionBy ?? throw new ArgumentNullException(nameof(partitionBy));
         _orderBy = orderBy ?? throw new ArgumentNullException(nameof(orderBy));

         if (orderBy.Count <= 0)
            throw new ArgumentException("Order By clause must contain at least one column.", nameof(orderBy));
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (!(visitor is QuerySqlGenerator))
            return base.Accept(visitor);

         visitor.Visit(_expressionFactory.Fragment("ROW_NUMBER() OVER("));

         if (_partitionBy.Count > 0)
         {
            visitor.Visit(_expressionFactory.Fragment("PARTITION BY "));
            RenderColumns(visitor, _partitionBy);
         }

         visitor.Visit(_expressionFactory.Fragment(" ORDER BY "));
         RenderColumns(visitor, _orderBy);

         visitor.Visit(_expressionFactory.Fragment(")"));

         return this;
      }

      private void RenderColumns([NotNull] ExpressionVisitor visitor, [NotNull] IEnumerable<Expression> columns)
      {
         var insertComma = false;

         foreach (var column in columns)
         {
            if (insertComma)
               visitor.Visit(_expressionFactory.Fragment(", "));

            visitor.Visit(column);
            insertComma = true;
         }
      }

      /// <inheritdoc />
      public override void Print(ExpressionPrinter expressionPrinter)
      {
         expressionPrinter.Append("ROW_NUMBER() OVER(");

         if (_partitionBy.Count > 0)
         {
            expressionPrinter.Append("PARTITION BY ");
            expressionPrinter.VisitList(_partitionBy);
            expressionPrinter.Append(" ");
         }

         expressionPrinter.Append("ORDER BY ");
         expressionPrinter.VisitList(_orderBy);
         expressionPrinter.Append(")");
      }

      /// <inheritdoc />
      [NotNull]
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
         var visitedPartitionBy = visitor.VisitExpressions(_partitionBy);
         var visitedOrderBy = visitor.VisitExpressions(_orderBy);

         if (ReferenceEquals(_orderBy, visitedOrderBy) && ReferenceEquals(visitedPartitionBy, _partitionBy))
            return this;

         return new RowNumberExpression(_expressionFactory, visitedPartitionBy, visitedOrderBy);
      }
   }
}
