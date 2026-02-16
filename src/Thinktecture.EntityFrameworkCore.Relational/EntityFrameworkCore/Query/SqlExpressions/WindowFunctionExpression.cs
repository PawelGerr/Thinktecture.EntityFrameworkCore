using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Window function expression.
/// </summary>
public class WindowFunctionExpression : SqlExpression
{
   private static readonly ConstructorInfo _quotingConstructor = typeof(WindowFunctionExpression).GetConstructors().Single();

   /// <summary>
   /// Creates a new instance of the <see cref="WindowFunctionExpression" /> class.
   /// </summary>
   /// <param name="name">Function name.</param>
   /// <param name="useAsteriskWhenNoArguments">Indication whether to use '*' when no arguments are provided.</param>
   /// <param name="type">Return type.</param>
   /// <param name="arguments">Arguments.</param>
   /// <param name="typeMapping">Type mapping.</param>
   /// <param name="partitions">A list expressions to partition by.</param>
   /// <param name="orderings">A list of ordering expressions to order by.</param>
   public WindowFunctionExpression(
      string name,
      bool useAsteriskWhenNoArguments,
      Type type,
      RelationalTypeMapping? typeMapping,
      IReadOnlyList<SqlExpression> arguments,
      IReadOnlyList<SqlExpression>? partitions,
      IReadOnlyList<OrderingExpression>? orderings)
      : base(type, typeMapping)
   {
      Name = name;
      UseAsteriskWhenNoArguments = useAsteriskWhenNoArguments;
      Arguments = arguments;
      Partitions = partitions ?? Array.Empty<SqlExpression>();
      Orderings = orderings ?? Array.Empty<OrderingExpression>();
   }

   /// <summary>
   /// Function name.
   /// </summary>
   public string Name { get; }

   /// <summary>
   /// Indication whether to use '*' when no arguments are provided.
   /// </summary>
   public bool UseAsteriskWhenNoArguments { get; }

   /// <summary>
   /// Function arguments.
   /// </summary>
   public IReadOnlyList<SqlExpression> Arguments { get; }

   /// <summary>
   ///     The list of expressions used in partitioning.
   /// </summary>
   public virtual IReadOnlyList<SqlExpression> Partitions { get; }

   /// <summary>
   ///     The list of ordering expressions used to order inside the given partition.
   /// </summary>
   public virtual IReadOnlyList<OrderingExpression> Orderings { get; }

   /// <inheritdoc />
   protected override Expression VisitChildren(ExpressionVisitor visitor)
   {
      var arguments = visitor.VisitExpressions(Arguments);
      var partitions = visitor.VisitExpressions(Partitions);
      var orderings = visitor.VisitExpressions(Orderings);

      return !ReferenceEquals(arguments, Arguments) || !ReferenceEquals(partitions, Partitions) || !ReferenceEquals(orderings, Orderings)
                ? new WindowFunctionExpression(Name, UseAsteriskWhenNoArguments, Type, TypeMapping, arguments, partitions, orderings)
                : this;
   }

   /// <inheritdoc />
   [Experimental("EF9100")]
   public override Expression Quote()
   {
      return New(_quotingConstructor,
                 Constant(Name),
                 Constant(UseAsteriskWhenNoArguments),
                 Constant(Type),
                 RelationalExpressionQuotingUtilities.QuoteTypeMapping(TypeMapping),
                 NewArrayInit(typeof(SqlExpression), Arguments.Select(a => a.Quote())),
                 NewArrayInit(typeof(SqlExpression), Partitions.Select(p => p.Quote())),
                 NewArrayInit(typeof(OrderingExpression), Orderings.Select(o => o.Quote())));
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      expressionPrinter.Append(Name).Append(" (");

      if (Arguments.Count != 0)
      {
         expressionPrinter.VisitCollection(Arguments);
      }
      else if (UseAsteriskWhenNoArguments)
      {
         expressionPrinter.Append(" * ");
      }

      expressionPrinter.Append(") OVER(");

      if (Partitions.Count != 0)
      {
         expressionPrinter.Append("PARTITION BY ");
         expressionPrinter.VisitCollection(Partitions);
         expressionPrinter.Append(" ");
      }

      if (Orderings.Count != 0)
      {
         expressionPrinter.Append("ORDER BY ");
         expressionPrinter.VisitCollection(Orderings);
      }

      expressionPrinter.Append(")");
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null
             && (ReferenceEquals(this, obj) || obj is WindowFunctionExpression windowFunctionExpression && Equals(windowFunctionExpression));
   }

   private bool Equals(WindowFunctionExpression windowFunctionExpression)
   {
      return base.Equals(windowFunctionExpression)
             && Name.Equals(windowFunctionExpression.Name)
             && UseAsteriskWhenNoArguments.Equals(windowFunctionExpression.UseAsteriskWhenNoArguments)
             && (Arguments == null ? windowFunctionExpression.Arguments == null : Arguments.SequenceEqual(windowFunctionExpression.Arguments))
             && (Partitions == null ? windowFunctionExpression.Partitions == null : Partitions.SequenceEqual(windowFunctionExpression.Partitions))
             && (Orderings == null ? windowFunctionExpression.Orderings == null : Orderings.SequenceEqual(windowFunctionExpression.Orderings));
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hash = new HashCode();
      hash.Add(base.GetHashCode());
      hash.Add(Name);
      hash.Add(UseAsteriskWhenNoArguments);

      foreach (var argument in Arguments)
      {
         hash.Add(argument);
      }

      foreach (var partition in Partitions)
      {
         hash.Add(partition);
      }

      foreach (var ordering in Orderings)
      {
         hash.Add(ordering);
      }

      return hash.ToHashCode();
   }
}
