using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;

namespace Thinktecture.EntityFrameworkCore.Query.Expressions
{
   /// <summary>
   /// Expression rendering "ROW_NUMBER()".
   /// </summary>
   public class RowNumberExpression : Expression
   {
      private readonly IReadOnlyCollection<Expression> _partitionBy;
      private readonly IReadOnlyCollection<Expression> _orderBy;

      /// <inheritdoc />
      public override ExpressionType NodeType => ExpressionType.Extension;

      /// <inheritdoc />
      public override bool CanReduce => false;

      /// <inheritdoc />
      public override Type Type => typeof(long);

      /// <summary>
      /// Initializes new instance of <see cref="RowNumberExpression"/>.
      /// </summary>
      /// <param name="partitionBy">Columns to partition by.</param>
      /// <param name="orderBy">Columns to order by.</param>
      public RowNumberExpression([NotNull] IReadOnlyCollection<Expression> partitionBy, [NotNull] IReadOnlyCollection<Expression> orderBy)
      {
         _partitionBy = partitionBy ?? throw new ArgumentNullException(nameof(partitionBy));
         _orderBy = orderBy ?? throw new ArgumentNullException(nameof(orderBy));

         if (orderBy.Count <= 0)
            throw new ArgumentException("Order By clause must contain at least one column.", nameof(orderBy));
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (!(visitor is IQuerySqlGenerator))
            return base.Accept(visitor);

         visitor.Visit(new SqlFragmentExpression("ROW_NUMBER() OVER("));

         if (_partitionBy.Count > 0)
         {
            visitor.Visit(new SqlFragmentExpression("PARTITION BY "));
            RenderColumns(visitor, _partitionBy);
         }

         visitor.Visit(new SqlFragmentExpression(" ORDER BY "));
         RenderColumns(visitor, _orderBy);

         visitor.Visit(new SqlFragmentExpression(")"));

         return this;
      }

      private static void RenderColumns([NotNull] ExpressionVisitor visitor, [NotNull] IEnumerable<Expression> columns)
      {
         var insertComma = false;

         foreach (var column in columns)
         {
            if (insertComma)
               visitor.Visit(new SqlFragmentExpression(", "));

            visitor.Visit(column);
            insertComma = true;
         }
      }

      /// <inheritdoc />
      [NotNull]
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
         var visitedPartitionBy = visitor.VisitExpressions(_partitionBy);
         var visitedOrderBy = visitor.VisitExpressions(_orderBy);

         if (ReferenceEquals(_orderBy, visitedOrderBy) && ReferenceEquals(visitedPartitionBy, _partitionBy))
            return this;

         return new RowNumberExpression(visitedPartitionBy, visitedOrderBy);
      }
   }
}
