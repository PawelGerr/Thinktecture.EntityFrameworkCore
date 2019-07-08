using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Executes bulk operations.
   /// </summary>
   public class SqlServerBulkOperationExecutor : ISqlServerBulkOperationExecutor
   {
      /// <inheritdoc />
      public async Task BulkInsertAsync<T>(DbContext ctx,
                                           IEnumerable<T> entities,
                                           string tableName,
                                           SqlBulkInsertOptions options,
                                           CancellationToken cancellationToken = default)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));
         if (options == null)
            throw new ArgumentNullException(nameof(options));

         var factory = ctx.GetService<IEntityDataReaderFactory>();
         var entityType = ctx.GetEntityType<T>();
         var sqlCon = (SqlConnection)ctx.Database.GetDbConnection();
         var sqlTx = (SqlTransaction)ctx.Database.CurrentTransaction?.GetDbTransaction();

         using (var reader = factory.Create(entities, entityType))
         using (var bulkCopy = new SqlBulkCopy(sqlCon, options.SqlBulkCopyOptions, sqlTx))
         {
            bulkCopy.DestinationTableName = $"[{tableName}]";
            bulkCopy.EnableStreaming = options.EnableStreaming;

            if (options.BulkCopyTimeout.HasValue)
               bulkCopy.BulkCopyTimeout = (int)options.BulkCopyTimeout.Value.TotalSeconds;

            if (options.BatchSize.HasValue)
               bulkCopy.BatchSize = options.BatchSize.Value;

            foreach (var property in entityType.GetProperties())
            {
               var relational = property.Relational();
               var index = reader.GetPropertyIndex(property.PropertyInfo);
               bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(index, relational.ColumnName));
            }

            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
         }
      }
   }
}
