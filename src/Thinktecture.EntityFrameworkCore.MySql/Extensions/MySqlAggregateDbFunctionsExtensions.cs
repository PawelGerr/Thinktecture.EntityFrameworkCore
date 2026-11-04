// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// MySQL aggregate function extensions for <see cref="DbFunctions" />.
/// </summary>
public static class MySqlAggregateDbFunctionsExtensions
{
   /// <summary>
   /// Translates to MySQL <c>BIT_OR</c> over the current group.
   /// </summary>
   /// <typeparam name="T">Type of the aggregated values.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="input">The values of the current group.</param>
   /// <returns>The bitwise OR of the values in the group.</returns>
   public static T BitOr<T>(this DbFunctions functions, IEnumerable<T> input)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Translates to MySQL <c>BIT_AND</c> over the current group.
   /// </summary>
   /// <typeparam name="T">Type of the aggregated values.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="input">The values of the current group.</param>
   /// <returns>The bitwise AND of the values in the group.</returns>
   public static T BitAnd<T>(this DbFunctions functions, IEnumerable<T> input)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Translates to MySQL <c>BIT_XOR</c> over the current group.
   /// </summary>
   /// <typeparam name="T">Type of the aggregated values.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="input">The values of the current group.</param>
   /// <returns>The bitwise XOR of the values in the group.</returns>
   public static T BitXor<T>(this DbFunctions functions, IEnumerable<T> input)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }
}