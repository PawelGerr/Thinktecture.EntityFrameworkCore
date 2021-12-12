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
   /// <param name="ctx">An instance of <see cref="DbContext"/> to use the values with.</param>
   /// <param name="values">A collection of values to create a query from.</param>
   /// <typeparam name="T">Type of the values.</typeparam>
   /// <returns>An <see cref="IQueryable{T}"/> giving access to the provided <paramref name="values"/>.</returns>
   IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IEnumerable<T> values);
}
