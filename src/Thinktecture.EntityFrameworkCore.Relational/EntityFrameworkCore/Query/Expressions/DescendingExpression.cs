using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;

namespace Thinktecture.EntityFrameworkCore.Query.Expressions
{
   /// <summary>
   /// Represents sort order 'DESC'
   /// </summary>
   public class DescendingExpression : Expression
   {
      /// <summary>
      /// Column to sort descending by.
      /// </summary>
      public Expression Column { get; }

      /// <inheritdoc />
      public override Type Type => Column.Type;

      /// <inheritdoc />
      public override bool CanReduce => false;

      /// <inheritdoc />
      public override ExpressionType NodeType => ExpressionType.Extension;

      /// <summary>
      /// Initializes new instance of <see cref="DescendingExpression"/>.
      /// </summary>
      /// <param name="column">Column to order by descending.</param>
      public DescendingExpression([NotNull] Expression column)
      {
         Column = column ?? throw new ArgumentNullException(nameof(column));
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (!(visitor is IQuerySqlGenerator))
            return base.Accept(visitor);

         visitor.Visit(Column);
         visitor.Visit(new SqlFragmentExpression(" DESC"));

         return this;
      }

      /// <inheritdoc />
      [NotNull]
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
         var visitedColumn = visitor.Visit(Column);

         if (visitedColumn != Column)
            return new DescendingExpression(visitedColumn);

         return this;
      }
   }
}
