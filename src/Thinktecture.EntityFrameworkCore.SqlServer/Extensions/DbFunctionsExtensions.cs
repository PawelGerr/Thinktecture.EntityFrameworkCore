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
      public static object Descending(this DbFunctions _, object column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <param name="_">An instance of <see cref="DbFunctions"/>.</param>
      /// <param name="partitionBy">A column or an object containing columns to partition by.</param>
      /// <param name="orderBy">A column or an object containing columns to order by.</param>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber(this DbFunctions _, object partitionBy, object orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <param name="_">An instance of <see cref="DbFunctions"/>.</param>
      /// <param name="orderBy">A column or an object containing columns to order by.</param>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber(this DbFunctions _, object orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }
   }
}
