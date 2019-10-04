using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.Expressions
{
   /// <summary>
   /// Represents sort order 'DESC'
   /// </summary>
   public class DescendingExpression : SqlExpression
   {
      private readonly ISqlExpressionFactory _expressionFactory;

      /// <summary>
      /// Column to sort descending by.
      /// </summary>
      public SqlExpression Column { get; }

      /// <inheritdoc />
      public override bool CanReduce => false;

      /// <summary>
      /// Initializes new instance of <see cref="DescendingExpression"/>.
      /// </summary>
      /// <param name="expressionFactory">Expression factory.</param>
      /// <param name="column">Column to order by descending.</param>
      public DescendingExpression([NotNull] ISqlExpressionFactory expressionFactory, [NotNull] SqlExpression column)
         : base(column.Type, RelationalTypeMapping.NullMapping)
      {
         _expressionFactory = expressionFactory ?? throw new ArgumentNullException(nameof(expressionFactory));
         Column = column ?? throw new ArgumentNullException(nameof(column));
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (!(visitor is QuerySqlGenerator))
            return base.Accept(visitor);

         visitor.Visit(Column);
         visitor.Visit(_expressionFactory.Fragment(" DESC"));

         return this;
      }

      /// <inheritdoc />
      [NotNull]
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
         var visitedColumn = (SqlExpression)visitor.Visit(Column);

         if (visitedColumn != Column)
            return new DescendingExpression(_expressionFactory, visitedColumn);

         return this;
      }

      /// <inheritdoc />
      public override void Print(ExpressionPrinter expressionPrinter)
      {
         expressionPrinter.Visit(Column);
         expressionPrinter.Append(" DESC");
      }
   }
}
