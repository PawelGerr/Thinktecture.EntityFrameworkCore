using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQL Server.
   /// </summary>
   public interface ISqlServerBulkInsertOptions : IBulkInsertOptions
   {
      /// <summary>
      /// Timeout used by <see cref="SqlBulkCopy"/>
      /// </summary>
      TimeSpan? BulkCopyTimeout { get; }

      /// <summary>
      /// Options used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      SqlBulkCopyOptions SqlBulkCopyOptions { get; }

      /// <summary>
      /// Batch size used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      int? BatchSize { get; }

      /// <summary>
      /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
      /// Default is set to <c>true</c>.
      /// </summary>
      bool EnableStreaming { get; }
   }
}
