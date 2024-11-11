using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Accumulator for orderings.
/// </summary>
public sealed class WindowFunctionOrderingsExpression : SqlExpression, INotNullableSqlExpression
{
   private static readonly ConstructorInfo _quotingConstructor = typeof(WindowFunctionOrderingsExpression).GetConstructors().Single();

   /// <summary>
   /// Orderings.
   /// </summary>
   public IReadOnlyList<OrderingExpression> Orderings { get; }

   /// <inheritdoc />
   public WindowFunctionOrderingsExpression(IReadOnlyList<OrderingExpression> orderings)
      : base(typeof(WindowFunctionOrderByClause), RelationalTypeMapping.NullMapping)
   {
      Orderings = orderings ?? throw new ArgumentNullException(nameof(orderings));
   }

   /// <inheritdoc />
   protected override Expression Accept(ExpressionVisitor visitor)
   {
      if (visitor is QuerySqlGenerator)
         throw new NotSupportedException("The window function contains some expressions not supported by the Entity Framework. One of the reason is the creation of new objects like: 'new { e.MyProperty, e.MyOtherProperty }'.");

      return base.Accept(visitor);
   }

   /// <inheritdoc />
   protected override Expression VisitChildren(ExpressionVisitor visitor)
   {
      var visited = visitor.VisitExpressions(Orderings);

      return ReferenceEquals(visited, Orderings) ? this : new WindowFunctionOrderingsExpression(visited);
   }

   /// <inheritdoc />
   public override Expression Quote()
   {
      return New(_quotingConstructor,
                 NewArrayInit(typeof(OrderingExpression), initializers: Orderings.Select(o => o.Quote())));
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      ArgumentNullException.ThrowIfNull(expressionPrinter);

      expressionPrinter.VisitCollection(Orderings);
   }

   /// <summary>
   /// Adds provided <paramref name="orderings"/> to existing <see cref="Orderings"/> and returns a new <see cref="WindowFunctionOrderingsExpression"/>.
   /// </summary>
   /// <param name="orderings">Orderings to add.</param>
   /// <returns>New instance of <see cref="WindowFunctionOrderingsExpression"/>.</returns>
   public WindowFunctionOrderingsExpression AddColumns(IEnumerable<OrderingExpression> orderings)
   {
      ArgumentNullException.ThrowIfNull(orderings);

      return new WindowFunctionOrderingsExpression(Orderings.Concat(orderings).ToList());
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null && (ReferenceEquals(this, obj) || Equals(obj as WindowFunctionOrderingsExpression));
   }

   private bool Equals(WindowFunctionOrderingsExpression? expression)
   {
      return base.Equals(expression) && Orderings.SequenceEqual(expression.Orderings);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hash = new HashCode();
      hash.Add(base.GetHashCode());

      for (var i = 0; i < Orderings.Count; i++)
      {
         hash.Add(Orderings[i]);
      }

      return hash.ToHashCode();
   }
}
