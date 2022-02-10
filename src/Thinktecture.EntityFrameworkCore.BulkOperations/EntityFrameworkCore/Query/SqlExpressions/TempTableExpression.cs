using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// An expression that represents a temp table.
/// </summary>
public sealed class TempTableExpression : TableExpressionBase, INotNullableSqlExpression
{
   /// <summary>
   /// The name of the table or view.
   /// </summary>
   public string Name { get; }

   /// <inheritdoc />
   [NotNull]
   public override string? Alias => base.Alias!;

   /// <summary>
   /// Initializes new instance of <see cref="TempTableExpression"/>.
   /// </summary>
   /// <param name="name">The name of the temp table.</param>
   /// <param name="alias">The alias of the temp table.</param>
   public TempTableExpression(string name, string? alias)
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
   public override bool Equals(object? obj)
   {
      // This should be reference equal only.
      return obj != null && ReferenceEquals(this, obj);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      return HashCode.Combine(base.GetHashCode(), Name);
   }
}
