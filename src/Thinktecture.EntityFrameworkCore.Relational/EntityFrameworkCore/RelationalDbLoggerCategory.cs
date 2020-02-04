using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Thinktecture.EntityFrameworkCore
{
   /// <summary>
   /// Logger category.
   /// </summary>
   public static class RelationalDbLoggerCategory
   {
      /// <summary>
      /// Logger category for nested transactions.
      /// </summary>
      [SuppressMessage("ReSharper", "CA1034")]
      public class NestedTransaction : LoggerCategory<NestedTransaction>
      {
      }
   }
}
