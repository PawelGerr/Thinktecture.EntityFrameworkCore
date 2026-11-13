using Thinktecture.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Windowed aggregate functions for MySQL.
/// </summary>
public static class MySqlWindowAggregateDbFunctionsExtensions
{
   private const string _IN_MEMORY_ERROR = "This method is for use with Entity Framework Core only.";

   /// <summary>Defines SUM(...) OVER (PARTITION BY ...).</summary>
   /// <typeparam name="TAggregate">The aggregate argument and return type.</typeparam>
   /// <typeparam name="TColumn1">The partition column type.</typeparam>
   /// <param name="_">The EF functions marker.</param>
   /// <param name="columnToAggregate">The value to aggregate.</param>
   /// <param name="partitionByColumn1">The partition column.</param>
   /// <returns>The windowed sum.</returns>
   public static TAggregate Sum<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Sum{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Sum<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }

   /// <summary>Defines AVG(...) OVER (PARTITION BY ...).</summary>
   /// <typeparam name="TAggregate">The aggregate argument and return type.</typeparam>
   /// <typeparam name="TColumn1">The partition column type.</typeparam>
   /// <param name="_">The EF functions marker.</param>
   /// <param name="columnToAggregate">The value to aggregate.</param>
   /// <param name="partitionByColumn1">The partition column.</param>
   /// <returns>The windowed average.</returns>
   public static TAggregate Average<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Average{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Average<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }

   /// <summary>Defines MAX(...) OVER (PARTITION BY ...).</summary>
   /// <typeparam name="TAggregate">The aggregate argument and return type.</typeparam>
   /// <typeparam name="TColumn1">The partition column type.</typeparam>
   /// <param name="_">The EF functions marker.</param>
   /// <param name="columnToAggregate">The value to aggregate.</param>
   /// <param name="partitionByColumn1">The partition column.</param>
   /// <returns>The windowed maximum.</returns>
   public static TAggregate Max<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Max{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Max<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }

   /// <summary>Defines MIN(...) OVER (PARTITION BY ...).</summary>
   /// <typeparam name="TAggregate">The aggregate argument and return type.</typeparam>
   /// <typeparam name="TColumn1">The partition column type.</typeparam>
   /// <param name="_">The EF functions marker.</param>
   /// <param name="columnToAggregate">The value to aggregate.</param>
   /// <param name="partitionByColumn1">The partition column.</param>
   /// <returns>The windowed minimum.</returns>
   public static TAggregate Min<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
   /// <inheritdoc cref="Min{TAggregate, TColumn1}(DbFunctions, TAggregate, TColumn1)" />
   public static TAggregate Min<TAggregate, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, TAggregate columnToAggregate, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderByClause) {
      throw new InvalidOperationException(_IN_MEMORY_ERROR);
   }
}
