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
   public static IQueryable<T> FromTempTable<T>(this IQueryable<T> source, string name)
   {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(name);

      var methodInfo = _fromTempTable.MakeGenericMethod(typeof(T));
      var expression = Expression.Call(null, methodInfo, source.Expression, new TempTableNameExpression(name));

      return source.Provider.CreateQuery<T>(expression);
   }
}
