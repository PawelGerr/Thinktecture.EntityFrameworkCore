using System.Linq;

namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <summary>
/// Factory for creation of an <see cref="IQueryable{T}"/> out of provided values.
/// </summary>
public interface ICollectionParameterFactory
{
   /// <summary>
   /// Creates an <see cref="IQueryable{T}"/> out of provided <paramref name="values"/>.
   /// </summary>
   /// <param name="ctx">An instance of <see cref="DbContext"/> to use the <paramref name="values"/> with.</param>
   /// <param name="values">A collection of <paramref name="values"/> to create a query from.</param>
   /// <param name="applyDistinct">Indication whether the query should apply 'DISTINCT' on <paramref name="values"/>.</param>
   /// <typeparam name="T">Type of the <paramref name="values"/>.</typeparam>
   /// <returns>An <see cref="IQueryable{T}"/> giving access to the provided <paramref name="values"/>.</returns>
   IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct);

   /// <summary>
   /// Creates an <see cref="IQueryable{T}"/> out of provided <paramref name="objects"/>.
   /// </summary>
   /// <param name="ctx">An instance of <see cref="DbContext"/> to use the <paramref name="objects"/> with.</param>
   /// <param name="objects">A collection of <paramref name="objects"/> to create a query from.</param>
   /// <param name="applyDistinct">Indication whether the query should apply 'DISTINCT' on <paramref name="objects"/>.</param>
   /// <typeparam name="T">Type of the <paramref name="objects"/>.</typeparam>
   /// <returns>An <see cref="IQueryable{T}"/> giving access to the provided <paramref name="objects"/>.</returns>
   IQueryable<T> CreateComplexQuery<T>(DbContext ctx, IReadOnlyCollection<T> objects, bool applyDistinct)
      where T : class;
}
