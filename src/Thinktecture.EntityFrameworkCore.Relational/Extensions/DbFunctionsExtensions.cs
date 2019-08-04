using System;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbFunctions"/>.
   /// </summary>
   public static class DbFunctionsExtensions
   {
      /// <summary>
      /// Changes the ordering to descending.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <param name="_">An instance of <see cref="DbFunctions"/>.</param>
      /// <param name="column">Column to order descending by.</param>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      // ReSharper disable UnusedParameter.Global
      public static object Descending(this DbFunctions _, object column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }
      // ReSharper restore UnusedParameter.Global
   }
}
