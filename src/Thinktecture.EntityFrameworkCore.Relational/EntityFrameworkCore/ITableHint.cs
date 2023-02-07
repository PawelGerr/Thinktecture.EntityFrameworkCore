using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Represents a table hint.
/// </summary>
public interface ITableHint
{
   /// <summary>
   /// Returns a string representing the table hint.
   /// </summary>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <returns>Table hint.</returns>
   string ToString(ISqlGenerationHelper sqlGenerationHelper);
}
