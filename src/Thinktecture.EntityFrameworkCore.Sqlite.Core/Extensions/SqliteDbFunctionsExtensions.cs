using Thinktecture.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbFunctions"/> for SQLite.
/// </summary>
public static class SqliteDbFunctionsExtensions
{
   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ()
   /// </summary>
   /// <param name="_">An instance of <see cref="DbFunctions"/>.</param>
   /// <param name="bucketCount">Number of buckets to distribute rows across.</param>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile(this DbFunctions _, int bucketCount)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile(this DbFunctions _, int bucketCount, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3, TColumn4>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }

   /// <summary>
   /// Definition of NTILE(bucketCount) OVER ( PARTITION BY ... ORDER BY ...)
   /// </summary>
   /// <exception cref="InvalidOperationException">Is thrown if executed in-memory.</exception>
   public static int NTile<TColumn1, TColumn2, TColumn3, TColumn4, TColumn5>(this DbFunctions _, int bucketCount, TColumn1 partitionByColumn1, TColumn2 partitionByColumn2, TColumn3 partitionByColumn3, TColumn4 partitionByColumn4, TColumn5 partitionByColumn5, WindowFunctionOrderByClause orderBy)
   {
      throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
   }
}
