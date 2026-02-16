using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Logger category.
/// </summary>
public static class NpgsqlDbLoggerCategory
{
   /// <summary>
   /// Logger category for bulk operations.
   /// </summary>
   public class BulkOperation : LoggerCategory<BulkOperation>
   {
   }
}
