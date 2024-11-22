using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore;

/// <summary>
/// Logger category.
/// </summary>
public static class RelationalDbLoggerCategory
{
   /// <summary>
   /// Logger category for nested transactions.
   /// </summary>
   public class NestedTransaction : LoggerCategory<NestedTransaction>
   {
   }
}
