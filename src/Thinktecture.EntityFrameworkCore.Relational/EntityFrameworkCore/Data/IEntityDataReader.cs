using System.Data;
using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Data reader to be used for bulk inserts.
/// </summary>
public interface IEntityDataReader : IDataReader
{
   /// <summary>
   /// Gets the properties the reader is created for.
   /// </summary>
   /// <returns>A collection of <see cref="PropertyInfo"/>.</returns>
   IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <summary>
   /// Gets the number of rows read so far.
   /// </summary>
   int RowsRead { get; }

   /// <summary>
   /// Gets the index of the provided <paramref name="property"/> that matches with the one of <see cref="IDataRecord.GetValue"/>.
   /// </summary>
   /// <param name="property">Property to get the index for.</param>
   /// <returns>Index of the property.</returns>
   int GetPropertyIndex(PropertyWithNavigations property);
}

/// <summary>
/// Data reader to be used for bulk inserts.
/// </summary>
public interface IEntityDataReader<out T> : IEntityDataReader
{
   /// <summary>
   /// Gets the entities that are read by now.
   /// This method should not be called until the end of the reading by the reader otherwise the returned collection will be incomplete!
   /// </summary>
   /// <returns>Read entities.</returns>
   IReadOnlyList<T> GetReadEntities();
}
