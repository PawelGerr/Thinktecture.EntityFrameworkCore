using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions
{
   /// <summary>
   /// Accumulator for orderings.
   /// </summary>
   public class RowNumberClauseOrderingsExpression : SqlExpression
   {
      /// <summary>
      /// Orderings.
      /// </summary>
      public IReadOnlyList<OrderingExpression> Orderings { get; }

      /// <inheritdoc />
      public RowNumberClauseOrderingsExpression(IReadOnlyList<OrderingExpression> orderings)
         : base(typeof(RowNumberOrderByClause), RelationalTypeMapping.NullMapping)
      {
         Orderings = orderings ?? throw new ArgumentNullException(nameof(orderings));
      }

      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (visitor is QuerySqlGenerator)
            throw new NotSupportedException($"The EF function '{nameof(SqlServerDbFunctionsExtensions.RowNumber)}' contains some expressions not supported by the Entity Framework. One of the reason is the creation of new objects like: 'new {{ e.MyProperty, e.MyOtherProperty }}'.");

         return base.Accept(visitor);
      }

      /// <inheritdoc />
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
         var visited = visitor.VisitExpressions(Orderings);

         return ReferenceEquals(visited, Orderings) ? this : new RowNumberClauseOrderingsExpression(visited);
      }

      /// <inheritdoc />
      public override void Print(ExpressionPrinter expressionPrinter)
      {
         expressionPrinter.VisitList(Orderings);
      }

      /// <summary>
      /// Adds provided <paramref name="orderings"/> to existing <see cref="Orderings"/> and returns a new <see cref="RowNumberClauseOrderingsExpression"/>.
      /// </summary>
      /// <param name="orderings">Orderings to add.</param>
      /// <returns>New instance of <see cref="RowNumberClauseOrderingsExpression"/>.</returns>
      public RowNumberClauseOrderingsExpression AddColumns(IEnumerable<OrderingExpression> orderings)
      {
         if (orderings == null)
            throw new ArgumentNullException(nameof(orderings));

         return new RowNumberClauseOrderingsExpression(Orderings.Concat(orderings).ToList());
      }
   }
}
