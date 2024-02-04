using Thinktecture.EntityFrameworkCore;

namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbFunctions"/>.
/// </summary>
public static class SqlServerDbFunctionsExtensions
{
   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Sum<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Sum<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of SUM(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Average<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Average<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of AVG(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Max<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Max<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MAX(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Min<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Min<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of MIN(...) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException"></exception>
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of a window function.
   /// <remarks>
   /// This method is for use with Entity Framework Core only and has no in-memory implementation.
   /// </remarks>
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions _, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }
}
