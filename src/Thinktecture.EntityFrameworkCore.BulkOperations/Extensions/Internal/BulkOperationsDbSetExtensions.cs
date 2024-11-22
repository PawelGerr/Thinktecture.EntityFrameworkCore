using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Thinktecture.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkOperationsDbSetExtensions
{
   private static readonly MethodInfo _fromTempTable = typeof(BulkOperationsDbSetExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                            .Single(m => m.Name == nameof(FromTempTable) && m.IsGenericMethod);

   /// <summary>
   /// This is an internal API.
   /// </summary>
   public static IQueryable<T> FromTempTable<T>(
      this IQueryable<T> source,
      TempTableInfo info)
   {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(info);

      var methodInfo = _fromTempTable.MakeGenericMethod(typeof(T));
      var expression = Expression.Call(null, methodInfo, source.Expression, new TempTableInfoExpression(info));

      return source.Provider.CreateQuery<T>(expression);
   }
}
