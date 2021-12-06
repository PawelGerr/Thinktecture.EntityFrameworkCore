using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Logger category.
/// </summary>
public static class SqlServerDbLoggerCategory
{
   /// <summary>
   /// Logger category for bulk operations.
   /// </summary>
   [SuppressMessage("ReSharper", "CA1034")]
   public class BulkOperation : LoggerCategory<BulkOperation>
   {
   }
}