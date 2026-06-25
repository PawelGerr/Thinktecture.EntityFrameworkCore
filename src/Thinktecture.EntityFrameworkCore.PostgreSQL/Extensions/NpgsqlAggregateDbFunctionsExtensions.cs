// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Aggregate (GROUP BY) function extensions for <see cref="DbFunctions"/> for PostgreSQL.
/// </summary>
public static class NpgsqlAggregateDbFunctionsExtensions
{
   private const string InMemoryError =
      "This method is for use with Entity Framework Core only and has no in-memory implementation.";

   /// <summary>
   /// Translates to the PostgreSQL <c>bit_or(...)</c> aggregate (bitwise OR over a group).
   /// </summary>
   /// <typeparam name="T">Type of the aggregated values (e.g. <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, a nullable form, or a <c>[Flags]</c> enum).</typeparam>
   /// <param name="_">The <see cref="DbFunctions"/> instance.</param>
   /// <param name="input">The values of the current group, e.g. <c>g.Select(e =&gt; e.Column)</c>.</param>
   /// <returns>The bitwise OR of all non-<c>null</c> values in the group.</returns>
   /// <exception cref="InvalidOperationException">Thrown if executed in-memory.</exception>
   public static T BitOr<T>(this DbFunctions _, IEnumerable<T> input)
   {
      throw new InvalidOperationException(InMemoryError);
   }

   /// <summary>
   /// Translates to the PostgreSQL <c>bit_and(...)</c> aggregate (bitwise AND over a group).
   /// </summary>
   /// <typeparam name="T">Type of the aggregated values (e.g. <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, a nullable form, or a <c>[Flags]</c> enum).</typeparam>
   /// <param name="_">The <see cref="DbFunctions"/> instance.</param>
   /// <param name="input">The values of the current group, e.g. <c>g.Select(e =&gt; e.Column)</c>.</param>
   /// <returns>The bitwise AND of all non-<c>null</c> values in the group.</returns>
   /// <exception cref="InvalidOperationException">Thrown if executed in-memory.</exception>
   public static T BitAnd<T>(this DbFunctions _, IEnumerable<T> input)
   {
      throw new InvalidOperationException(InMemoryError);
   }

   /// <summary>
   /// Translates to the PostgreSQL <c>bit_xor(...)</c> aggregate (bitwise XOR over a group).
   /// </summary>
   /// <remarks><c>bit_xor</c> requires PostgreSQL 14 or later.</remarks>
   /// <typeparam name="T">Type of the aggregated values (e.g. <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, a nullable form, or a <c>[Flags]</c> enum).</typeparam>
   /// <param name="_">The <see cref="DbFunctions"/> instance.</param>
   /// <param name="input">The values of the current group, e.g. <c>g.Select(e =&gt; e.Column)</c>.</param>
   /// <returns>The bitwise XOR of all non-<c>null</c> values in the group.</returns>
   /// <exception cref="InvalidOperationException">Thrown if executed in-memory.</exception>
   public static T BitXor<T>(this DbFunctions _, IEnumerable<T> input)
   {
      throw new InvalidOperationException(InMemoryError);
   }
}
