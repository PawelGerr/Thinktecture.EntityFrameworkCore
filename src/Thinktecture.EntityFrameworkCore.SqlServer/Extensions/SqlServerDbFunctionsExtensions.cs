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
}
