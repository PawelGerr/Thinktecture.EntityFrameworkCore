using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API. One target column of a query-based bulk update or insert after the expansion of whole complex properties.
/// </summary>
/// <param name="TargetProperty">The resolved target property.</param>
/// <param name="TargetPropertySelector">The target selector as written by the caller; used in error messages only.</param>
/// <param name="ValueSelector">The value selector, extended by the member accesses of the expansion.</param>
public sealed record ResolvedSetPropertyEntry(IProperty TargetProperty, LambdaExpression TargetPropertySelector, LambdaExpression ValueSelector);
