using System.Linq.Expressions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Builder for specifying property assignments in a query-based bulk update.
/// </summary>
/// <typeparam name="TTarget">The target entity type.</typeparam>
/// <typeparam name="TSource">The source entity type.</typeparam>
public sealed class SetPropertyBuilder<TTarget, TSource>
{
   private readonly List<ISetPropertyEntry> _entries = [];

   /// <summary>
   /// Gets the property assignments configured so far.
   /// </summary>
   public IReadOnlyList<ISetPropertyEntry> Entries => _entries;

   /// <summary>
   /// Specifies a property to set and the value expression to assign.
   /// </summary>
   /// <param name="targetPropertySelector">Expression selecting the target property.</param>
   /// <param name="valueSelector">Expression providing the value from the target and/or source entity.</param>
   /// <typeparam name="TProp">The property type.</typeparam>
   /// <returns>This builder for chaining.</returns>
   public SetPropertyBuilder<TTarget, TSource> Set<TProp>(
      Expression<Func<TTarget, TProp>> targetPropertySelector,
      Expression<Func<TTarget, TSource, TProp>> valueSelector)
   {
      ArgumentNullException.ThrowIfNull(targetPropertySelector);
      ArgumentNullException.ThrowIfNull(valueSelector);

      _entries.Add(new SetPropertyEntry(targetPropertySelector, valueSelector));

      return this;
   }
}
