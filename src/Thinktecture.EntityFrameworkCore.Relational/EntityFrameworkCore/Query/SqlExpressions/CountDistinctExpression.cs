using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions
{
   /// <summary>
   /// Translates to "COUNT( DISTINCT column )"
   /// </summary>
   public class CountDistinctExpression : SqlExpression
   {
      public SqlExpression Column { get; }

      /// <summary>
      /// Initializes new instance of <see cref="CountDistinctExpression"/>.
      /// </summary>
      /// <param name="column">Columns to distinct by.</param>
      /// <param name="typeMapping">Type mapping.</param>
      public CountDistinctExpression(SqlExpression column, RelationalTypeMapping typeMapping)
         : base(typeof(int), typeMapping)
      {
         Column = column ?? throw new ArgumentNullException(nameof(column));
      }

      /// <inheritdoc />
      protected override void Print(ExpressionPrinter expressionPrinter)
      {
         if (expressionPrinter == null)
            throw new ArgumentNullException(nameof(expressionPrinter));

         expressionPrinter.Append("COUNT(DISTINCT ");
         expressionPrinter.Visit(Column);
         expressionPrinter.Append(")");
      }

      /// <inheritdoc />
      protected override Expression VisitChildren(ExpressionVisitor visitor)
      {
#pragma warning disable CA1062
         var visitedColumn = visitor.Visit(Column);
#pragma warning restore CA1062

         return Update((SqlExpression)visitedColumn);
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         if (!(visitor is QuerySqlGenerator sqlGenerator))
            return base.Accept(visitor);

         sqlGenerator.Visit(new SqlFragmentExpression("COUNT(DISTINCT "));
         sqlGenerator.Visit(Column);
         sqlGenerator.Visit(new SqlFragmentExpression(")"));

         return this;
      }

      /// <inheritdoc />
      public override bool Equals(object obj)
      {
         return obj != null && (ReferenceEquals(this, obj) || Equals(obj as CountDistinctExpression));
      }

      private bool Equals(CountDistinctExpression? expression)
      {
         return base.Equals(expression) && Column.Equals(expression.Column);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return HashCode.Combine(base.GetHashCode(), Column);
      }

      public CountDistinctExpression Update(SqlExpression column)
      {
         return ReferenceEquals(Column, column)
                   ? this
                   : new CountDistinctExpression(column, TypeMapping);
      }
   }
}
