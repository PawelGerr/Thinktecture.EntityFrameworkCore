using Thinktecture.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// MySQL window-function extensions for <see cref="DbFunctions" />.
/// </summary>
public static class MySqlDbFunctionsExtensions
{
   /// <summary>
   /// Defines a window function call.
   /// </summary>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines a window function call with ordering.
   /// </summary>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines a window function call with one argument.
   /// </summary>
   /// <typeparam name="TArgument">The argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument">The argument.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TArgument, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, TArgument argument)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function call with two arguments.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function call with three arguments.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function call with four arguments.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function call with five arguments.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="T5">The fifth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="argument5">The fifth argument.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines a window function call with partitioning and ordering.
   /// </summary>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines a window function call with one argument, partitioning, and ordering.
   /// </summary>
   /// <typeparam name="TArgument">The argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument">The argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TArgument, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, TArgument argument, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function partitioned by columns.</summary>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with one argument partitioned by columns.</summary>
   /// <typeparam name="T1">The argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with two arguments partitioned by columns.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with three arguments partitioned by columns.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with four arguments partitioned by columns.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with five arguments partitioned by columns.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="T5">The fifth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="argument5">The fifth argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionPartitionByClause partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with one argument and ordering.</summary>
   /// <typeparam name="T1">The argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The argument.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with two arguments and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with three arguments and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with four arguments and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with five arguments and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="T5">The fifth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="argument5">The fifth argument.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with two arguments, partitioning, and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with three arguments, partitioning, and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with four arguments, partitioning, and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>Defines a window function with five arguments, partitioning, and ordering.</summary>
   /// <typeparam name="T1">The first argument type.</typeparam>
   /// <typeparam name="T2">The second argument type.</typeparam>
   /// <typeparam name="T3">The third argument type.</typeparam>
   /// <typeparam name="T4">The fourth argument type.</typeparam>
   /// <typeparam name="T5">The fifth argument type.</typeparam>
   /// <typeparam name="TResult">The result type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="windowFunction">The window function.</param>
   /// <param name="argument1">The first argument.</param>
   /// <param name="argument2">The second argument.</param>
   /// <param name="argument3">The third argument.</param>
   /// <param name="argument4">The fourth argument.</param>
   /// <param name="argument5">The fifth argument.</param>
   /// <param name="partitionBy">The partitioning clause.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The window function result.</returns>
   public static TResult WindowFunction<T1, T2, T3, T4, T5, TResult>(this DbFunctions functions, WindowFunction<TResult> windowFunction, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5, WindowFunctionPartitionByClause partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines an NTILE window function call.
   /// </summary>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="bucketCount">The number of buckets.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The bucket number.</returns>
   public static int NTile(this DbFunctions functions, int bucketCount, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines an NTILE window function call without ordering.
   /// </summary>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="bucketCount">The number of buckets.</param>
   /// <returns>The bucket number.</returns>
   public static int NTile(this DbFunctions functions, int bucketCount)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines an NTILE window function call partitioned by one column.
   /// </summary>
   /// <typeparam name="TPartition">The partition column type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="bucketCount">The number of buckets.</param>
   /// <param name="partitionBy">The partition column.</param>
   /// <returns>The bucket number.</returns>
   public static int NTile<TPartition>(this DbFunctions functions, int bucketCount, TPartition partitionBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <summary>
   /// Defines an NTILE window function call partitioned by one column and ordered by the provided clause.
   /// </summary>
   /// <typeparam name="TPartition">The partition column type.</typeparam>
   /// <param name="functions">The EF functions marker.</param>
   /// <param name="bucketCount">The number of buckets.</param>
   /// <param name="partitionBy">The partition column.</param>
   /// <param name="orderBy">The ordering clause.</param>
   /// <returns>The bucket number.</returns>
   public static int NTile<TPartition>(this DbFunctions functions, int bucketCount, TPartition partitionBy, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition)" />
   public static int NTile<TPartition1, TPartition2>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition, WindowFunctionOrderByClause)" />
   public static int NTile<TPartition1, TPartition2>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition)" />
   public static int NTile<TPartition1, TPartition2, TPartition3>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition, WindowFunctionOrderByClause)" />
   public static int NTile<TPartition1, TPartition2, TPartition3>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition)" />
   public static int NTile<TPartition1, TPartition2, TPartition3, TPartition4>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3, TPartition4 partitionBy4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition, WindowFunctionOrderByClause)" />
   public static int NTile<TPartition1, TPartition2, TPartition3, TPartition4>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3, TPartition4 partitionBy4, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition)" />
   public static int NTile<TPartition1, TPartition2, TPartition3, TPartition4, TPartition5>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3, TPartition4 partitionBy4, TPartition5 partitionBy5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }

   /// <inheritdoc cref="NTile{TPartition}(DbFunctions, int, TPartition, WindowFunctionOrderByClause)" />
   public static int NTile<TPartition1, TPartition2, TPartition3, TPartition4, TPartition5>(this DbFunctions functions, int bucketCount, TPartition1 partitionBy1, TPartition2 partitionBy2, TPartition3 partitionBy3, TPartition4 partitionBy4, TPartition5 partitionBy5, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only.");
   }
}