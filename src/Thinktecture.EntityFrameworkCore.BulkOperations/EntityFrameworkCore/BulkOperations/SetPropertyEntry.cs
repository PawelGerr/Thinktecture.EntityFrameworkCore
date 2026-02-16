using System.Linq.Expressions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed record SetPropertyEntry(LambdaExpression TargetPropertySelector, LambdaExpression ValueSelector)
   : ISetPropertyEntry;
