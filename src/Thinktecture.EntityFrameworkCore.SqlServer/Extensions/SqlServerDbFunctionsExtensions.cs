using System;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
#pragma warning disable CA1707, CA1801

namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbFunctions"/>.
   /// </summary>
   public static class SqlServerDbFunctionsExtensions
   {
      // ReSharper disable UnusedParameter.Global

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <param name="_">An instance of <see cref="DbFunctions"/>.</param>
      /// <param name="orderBy">A column or an object containing columns to order by.</param>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber(this DbFunctions _, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1>(this DbFunctions _, T1 partitionByColumn1, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, T12 partitionByColumn12, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, T12 partitionByColumn12, T13 partitionByColumn13, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, T12 partitionByColumn12, T13 partitionByColumn13, T14 partitionByColumn14, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, T12 partitionByColumn12, T13 partitionByColumn13, T14 partitionByColumn14, T15 partitionByColumn15, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ROW_NUMBER.
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// </summary>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static long RowNumber<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this DbFunctions _, T1 partitionByColumn1, T2 partitionByColumn2, T3 partitionByColumn3, T4 partitionByColumn4, T5 partitionByColumn5, T6 partitionByColumn6, T7 partitionByColumn7, T8 partitionByColumn8, T9 partitionByColumn9, T10 partitionByColumn10, T11 partitionByColumn11, T12 partitionByColumn12, T13 partitionByColumn13, T14 partitionByColumn14, T15 partitionByColumn15, T16 partitionByColumn16, RowNumberOrderByClause orderBy)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ORDER BY clause of a the ROW_NUMBER expression.
      /// </summary>
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static RowNumberOrderByClause OrderBy<T>(this DbFunctions _, T column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ORDER BY clause of a the ROW_NUMBER expression.
      /// </summary>
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static RowNumberOrderByClause OrderByDescending<T>(this DbFunctions _, T column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ORDER BY clause of a the ROW_NUMBER expression.
      /// </summary>
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static RowNumberOrderByClause ThenBy<T>(this RowNumberOrderByClause clause, T column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      /// <summary>
      /// Definition of the ORDER BY clause of a the ROW_NUMBER expression.
      /// </summary>
      /// <remarks>
      /// This method is for use with Entity Framework Core only and has no in-memory implementation.
      /// </remarks>
      /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
      public static RowNumberOrderByClause ThenByDescending<T>(this RowNumberOrderByClause clause, T column)
      {
         throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
      }

      // ReSharper restore UnusedParameter.Global
   }
}
