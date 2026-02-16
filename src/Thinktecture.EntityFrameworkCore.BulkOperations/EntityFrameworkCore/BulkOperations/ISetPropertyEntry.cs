using System.Linq.Expressions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Represents a single property assignment for a query-based bulk update.
/// </summary>
public interface ISetPropertyEntry
{
   /// <summary>
   /// Gets the expression selecting the target property to set.
   /// </summary>
   LambdaExpression TargetPropertySelector { get; }

   /// <summary>
   /// Gets the expression providing the value to assign.
   /// </summary>
   LambdaExpression ValueSelector { get; }
}
