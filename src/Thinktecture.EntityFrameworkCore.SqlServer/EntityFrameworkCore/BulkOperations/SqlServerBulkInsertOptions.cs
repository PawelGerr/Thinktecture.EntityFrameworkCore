using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by <see cref="SqlServerDbContextExtensions.BulkInsertAsync{T}"/>.
   /// </summary>
   public class SqlServerBulkInsertOptions : IBulkInsertOptions
   {
      /// <summary>
      /// Timeout used by <see cref="SqlBulkCopy"/>
      /// </summary>
      public TimeSpan? BulkCopyTimeout { get; set; }

      /// <summary>
      /// Options used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      public SqlBulkCopyOptions SqlBulkCopyOptions { get; set; }

      /// <summary>
      /// Batch size used by <see cref="SqlBulkCopy"/>.
      /// </summary>
      public int? BatchSize { get; set; }

      /// <summary>
      /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
      /// Default is set to <c>true</c>.
      /// </summary>
      public bool EnableStreaming { get; set; } = true;

      /// <inheritdoc />
      public IEntityMembersProvider? EntityMembersProvider { get; set; }
   }
}
