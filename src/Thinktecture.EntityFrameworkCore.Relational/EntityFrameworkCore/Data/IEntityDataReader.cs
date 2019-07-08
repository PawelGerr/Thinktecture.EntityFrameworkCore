using System.Data;
using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Data reader to be used for bulk inserts.
   /// </summary>
   public interface IEntityDataReader : IDataReader
   {
      /// <summary>
      /// Gets the index of the provided <paramref name="propertyInfo"/> used by <see cref="IDataRecord.GetValue"/>.
      /// </summary>
      /// <param name="propertyInfo">Property info to get index for.</param>
      /// <returns>Index of the property.</returns>
      int GetPropertyIndex(PropertyInfo propertyInfo);
   }
}
