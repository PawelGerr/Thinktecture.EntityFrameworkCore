using System.Linq.Expressions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Builder for specifying column mappings in a query-based bulk insert.
/// </summary>
/// <typeparam name="TTarget">The target entity type.</typeparam>
/// <typeparam name="TSource">The source entity type.</typeparam>
public sealed class InsertPropertyBuilder<TTarget, TSource>
{
   private readonly List<ISetPropertyEntry> _entries = [];

   /// <summary>
   /// Gets the column mappings configured so far.
   /// </summary>
   public IReadOnlyList<ISetPropertyEntry> Entries => _entries;

   /// <summary>
   /// Maps a target column to a source expression.
   /// </summary>
   /// <param name="targetColumnSelector">Expression selecting the target column.</param>
   /// <param name="sourceValueSelector">Expression providing the value from the source entity.</param>
   /// <typeparam name="TProp">The property type.</typeparam>
   /// <returns>This builder for chaining.</returns>
   public InsertPropertyBuilder<TTarget, TSource> Map<TProp>(
      Expression<Func<TTarget, TProp>> targetColumnSelector,
      Expression<Func<TSource, TProp>> sourceValueSelector)
   {
      ArgumentNullException.ThrowIfNull(targetColumnSelector);
      ArgumentNullException.ThrowIfNull(sourceValueSelector);

      _entries.Add(new SetPropertyEntry(targetColumnSelector, sourceValueSelector));

      return this;
   }
}
