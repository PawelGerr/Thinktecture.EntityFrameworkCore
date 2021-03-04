using System;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions
{
   /// <summary>
   /// An expression that represents a temp table.
   /// </summary>
   public sealed class TempTableExpression : TableExpressionBase
   {
      /// <summary>
      /// The name of the table or view.
      /// </summary>
      public string Name { get; }

      /// <summary>
      /// Initializes new instance of <see cref="TempTableExpression"/>.
      /// </summary>
      /// <param name="name">The name of the temp table.</param>
      /// <param name="alias">The alias of the temp table.</param>
      public TempTableExpression(string name, string alias)
         : base(alias)
      {
         Name = name;
      }

      /// <inheritdoc />
      protected override void Print(ExpressionPrinter expressionPrinter)
      {
         expressionPrinter.Append(Name).Append(" AS ").Append(Alias);
      }

      /// <inheritdoc />
      public override bool Equals(object obj)
      {
         return ReferenceEquals(this, obj) || obj is TempTableExpression tempTableExpression && Equals(tempTableExpression);
      }

      private bool Equals(TempTableExpression tempTableExpression)
      {
         return base.Equals(tempTableExpression) && string.Equals(Name, tempTableExpression.Name);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return HashCode.Combine(base.GetHashCode(), Name);
      }
   }
}
